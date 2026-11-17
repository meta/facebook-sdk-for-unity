#!/bin/sh
#
# Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
#
# You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
# copy, modify, and distribute this software in source code or binary form for use
# in connection with the web services and APIs provided by Facebook.
#
# As with any software that integrates with the Facebook platform, your use of
# this software is subject to the Facebook Developer Principles and Policies
# [http://developers.facebook.com/policy/]. This copyright notice shall be
# included in all copies or substantial portions of the software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
# FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
# COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
# IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
# CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

# Checks what Assets/Editor/FBXcodePostProcess.cs was supposed to write into an
# exported iOS Xcode project.
#
# A successful export proves nothing on its own: the post-process hooks catch and
# log their own failures, so Unity reports success while an artifact is silently
# missing. That is exactly how the Podfile app-target bug shipped.
#
# Usage: scripts/verify_ios_export.sh <path-to-exported-xcode-project>

EXPORT="$1"

if [ -z "$EXPORT" ] || [ ! -d "$EXPORT" ]; then
  echo "usage: $0 <path-to-exported-xcode-project>" >&2
  exit 2
fi

PASS=0
FAIL=0
PLISTBUDDY=/usr/libexec/PlistBuddy

ok()   { PASS=$((PASS+1)); printf '  \033[0;32mPASS\033[0m  %s\n' "$1"; }
bad()  { FAIL=$((FAIL+1)); printf '  \033[0;31mFAIL\033[0m  %s\n' "$1"; [ -n "$2" ] && printf '          %s\n' "$2"; }
info() { printf '        %s\n' "$1"; }

PBXPROJ=$(ls -d "$EXPORT"/*.xcodeproj/project.pbxproj 2>/dev/null | head -1)
if [ -z "$PBXPROJ" ]; then
  echo "No .xcodeproj found under $EXPORT" >&2
  exit 2
fi

PROJECT_NAME=$(basename "$(dirname "$PBXPROJ")" .xcodeproj)

# The Objective-C trampoline writes Classes/UnityAppController.mm; the Swift
# project type replaces it with UnityPlayer.
if [ -f "$EXPORT/Classes/UnityAppController.mm" ]; then
  PROJECT_TYPE="Objective-C"
else
  PROJECT_TYPE="Swift"
fi

# Read the app target out of the project rather than inferring it from the
# .xcodeproj filename. Both project types name the app target "Unity-iPhone";
# only the .xcodeproj file takes the Unity project name under Swift.
EXPECTED_TARGET=$(python3 -c "
import re,sys
s=open('$PBXPROJ',errors='ignore').read()
names=[m.group(1).strip().strip('\"')
       for m in re.finditer(r'isa = PBXNativeTarget;.*?name = ([^;]+);', s, re.S)]
app=[n for n in names if n not in ('UnityFramework','GameAssembly') and not n.endswith('Tests')]
print(app[0] if app else '')
" 2>/dev/null)

echo
echo "Export:       $EXPORT"
echo "Project:      $PROJECT_NAME.xcodeproj"
echo "Project type: $PROJECT_TYPE"
echo "App target:   $EXPECTED_TARGET"
echo

# --- 1. Facebook SDK Swift package -----------------------------------------
echo "Facebook SDK Swift package"
PACKAGE_URL="https://github.com/meta/facebook-ios-sdk"
PACKAGE_COUNT=$(grep -cE "repositoryURL = \"?${PACKAGE_URL}\"?;" "$PBXPROJ")
if [ "$PACKAGE_COUNT" -eq 1 ]; then
  ok "package reference present once: $PACKAGE_URL"
else
  bad "package reference present once: $PACKAGE_URL" \
      "found $PACKAGE_COUNT — 0 means the SDK is not added; more than 1 means an Append build added it again"
fi

# Each product must reach UnityFramework, where the plugin compiles, and the
# app target, or Xcode does not embed the SDK's dynamic frameworks in the app.
for TARGET in UnityFramework "$EXPECTED_TARGET"; do
  MISSING=$(python3 -c "
import re,sys
s=open('$PBXPROJ',errors='ignore').read()
def blocks(isa):
    return re.findall(r'([0-9A-Fa-f]{24,32})[^=\n]*= \{\s*isa = '+isa+r';(.*?)\n\s*\};', s, re.S)
products={g: re.search(r'productName = \"?([^\";]+)', b).group(1)
          for g,b in blocks('XCSwiftPackageProductDependency') if 'productName' in b}
linked=set()
for g,b in blocks('PBXNativeTarget'):
    name=re.search(r'\bname = \"?([^\";]+)', b)
    deps=re.search(r'packageProductDependencies = \((.*?)\);', b, re.S)
    if name and name.group(1)=='$TARGET' and deps:
        linked={products.get(d) for d in re.findall(r'[0-9A-Fa-f]{24,32}', deps.group(1))}
need=['FacebookCore','FacebookLogin','FacebookShare','FacebookGamingServices']
print(' '.join(p for p in need if p not in linked))
" 2>/dev/null)
  if [ -z "$MISSING" ]; then
    ok "$TARGET uses FacebookCore, FacebookLogin, FacebookShare, FacebookGamingServices"
  else
    bad "$TARGET uses all four Facebook SDK products" "missing: $MISSING"
  fi
done

PODFILE="$EXPORT/Podfile"
if [ -f "$PODFILE" ] && grep -qE "pod[[:space:]]+['\"]FBSDK" "$PODFILE"; then
  bad "no Facebook SDK pods in the Podfile" "the app would link the SDK twice, once from the pods and once from the package"
  info "$(grep -E "pod[[:space:]]+['\"]FBSDK" "$PODFILE" | tr -s ' ' | tr '\n' ';')"
else
  ok "no Facebook SDK pods in the Podfile"
fi

if [ ! -f "$PODFILE" ]; then
  ok "no Podfile (no plugin declares pods)"
elif ! grep -qE "^[[:space:]]*pod[[:space:]]+['\"]" "$PODFILE"; then
  bad "no empty Podfile" "External Dependency Manager wrote a Podfile with no pods: AllowEmptyPodfileGeneration was not turned off, or this is an Append build over an older export"
else
  info "Podfile present with pods from other plugins: $(grep -cE "^[[:space:]]*pod[[:space:]]+['\"]" "$PODFILE") pod(s)"
fi

# --- 2. Info.plist -----------------------------------------------------------
echo
echo "Info.plist"
if [ -f "$EXPORT/Info.plist" ]; then
  PLIST="$EXPORT/Info.plist"
elif [ -f "$EXPORT/$PROJECT_NAME/Info.plist" ]; then
  PLIST="$EXPORT/$PROJECT_NAME/Info.plist"
else
  PLIST=""
fi

if [ -z "$PLIST" ]; then
  bad "Info.plist located" "checked build root and $PROJECT_NAME/ — ResolveInfoPlistPath would also have failed"
else
  info "at ${PLIST#$EXPORT/}"
  for KEY in FacebookAppID FacebookClientToken; do
    VALUE=$($PLISTBUDDY -c "Print :$KEY" "$PLIST" 2>/dev/null)
    if [ -n "$VALUE" ]; then ok "$KEY = $VALUE"; else bad "$KEY present"; fi
  done

  SCHEME=$($PLISTBUDDY -c "Print :CFBundleURLTypes:0:CFBundleURLSchemes:0" "$PLIST" 2>/dev/null)
  APPID=$($PLISTBUDDY -c "Print :FacebookAppID" "$PLIST" 2>/dev/null)
  if [ "$SCHEME" = "fb$APPID" ]; then
    ok "CFBundleURLSchemes = $SCHEME"
  else
    bad "CFBundleURLSchemes = fb<appid>" "found '$SCHEME', expected 'fb$APPID' — Login redirects and deep links cannot reach the app"
  fi

  for S in fbapi fb-messenger-api fbauth2 fbshareextension; do
    if $PLISTBUDDY -c "Print :LSApplicationQueriesSchemes" "$PLIST" 2>/dev/null | grep -qw "$S"; then
      ok "LSApplicationQueriesSchemes contains $S"
    else
      bad "LSApplicationQueriesSchemes contains $S" "app-switch to the Facebook app will not be attempted"
    fi
  done
fi

# --- 3. Build settings on both targets ---------------------------------------
echo
echo "Build settings (project.pbxproj)"
if grep -q "Accelerate.framework" "$PBXPROJ"; then
  ok "Accelerate.framework linked"
else
  bad "Accelerate.framework linked"
fi

SWIFT_VERSIONS=$(grep -o "SWIFT_VERSION = [^;]*" "$PBXPROJ" | sort -u | sed 's/SWIFT_VERSION = //' | tr -d '"' | tr '\n' ' ')
if [ -n "$SWIFT_VERSIONS" ]; then
  case "$SWIFT_VERSIONS" in
    *" "*" "*) bad "SWIFT_VERSION consistent" "multiple values: $SWIFT_VERSIONS" ;;
    *)         ok "SWIFT_VERSION = $SWIFT_VERSIONS" ;;
  esac
  if echo "$SWIFT_VERSIONS" | grep -q " .*5\.0\|5 5"; then
    bad "SWIFT_VERSION not corrupted" "looks like the '5 5.0' concatenation from T110370082"
  fi
else
  bad "SWIFT_VERSION set"
fi

# --- 4. Injected macro must be gone ------------------------------------------
echo
echo "Regression checks"
STALE=$(grep -c "FB_UNITY_XCODE_PROJECT_TYPE_SWIFT" "$PBXPROJ")
if [ "$STALE" -eq 0 ]; then
  ok "FB_UNITY_XCODE_PROJECT_TYPE_SWIFT not injected (removed; Unity's own macro is used)"
else
  bad "FB_UNITY_XCODE_PROJECT_TYPE_SWIFT not injected" "found $STALE occurrences — stale export, or the removal regressed"
fi

STALE=$(grep -c "FBSDKCOCOAPODS" "$PBXPROJ")
if [ "$STALE" -eq 0 ]; then
  ok "FBSDKCOCOAPODS not injected (removed with the move to Swift Package Manager)"
else
  bad "FBSDKCOCOAPODS not injected" "found $STALE occurrences — stale export, or the removal regressed"
fi

DUPES=$(awk '/GCC_PREPROCESSOR_DEFINITIONS = \(/,/\);/' "$PBXPROJ" | grep -c '\$(inherited)')
TOTALBLOCKS=$(grep -c "GCC_PREPROCESSOR_DEFINITIONS = (" "$PBXPROJ")
if [ "$DUPES" -le "$TOTALBLOCKS" ]; then
  ok "no duplicate \$(inherited) ($DUPES across $TOTALBLOCKS blocks)"
else
  bad "no duplicate \$(inherited)" "$DUPES across $TOTALBLOCKS blocks — pod install will warn about overriding GCC_PREPROCESSOR_DEFINITIONS"
fi

# --- summary -----------------------------------------------------------------
echo
echo "-------------------------------------------"
printf '  %d passed, %d failed\n' "$PASS" "$FAIL"
echo
echo "  Not covered here:"
echo "    - run 'xcodebuild -resolvePackageDependencies' to confirm the package resolves"
echo "    - the native preprocessor guard needs the #error gate, not this script"
echo "-------------------------------------------"
echo

[ "$FAIL" -eq 0 ] || exit 1
