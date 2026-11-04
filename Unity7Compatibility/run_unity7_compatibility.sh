#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
sdk_root="$(cd "${script_dir}/.." && pwd)"
unity_path="${UNITY_PATH:-}"
results_dir="${RESULTS_DIR:-/tmp/facebook-sdk-unity7-compatibility-results}"
test_project="$(mktemp -d /tmp/facebook-sdk-unity7-compatibility-XXXXXX)"
keep_project="${KEEP_PROJECT:-0}"
package_registry_url="${UNITY_PACKAGE_REGISTRY_URL:-}"

cleanup() {
  if [[ "${keep_project}" == "1" ]]; then
    echo "Keeping staged Unity project: ${test_project}"
    return
  fi
  rm -rf "${test_project}" || true
}
trap cleanup EXIT

if [[ -z "${unity_path}" ]]; then
  echo "UNITY_PATH must point to a Unity 7000.0.0a7 executable" >&2
  exit 2
fi

if [[ ! -x "${unity_path}" ]]; then
  echo "Unity executable not found: ${unity_path}" >&2
  exit 2
fi

echo "Staging Unity 7 compatibility project: ${test_project}"

mkdir -p "${test_project}/Assets/FacebookSDKSource" "${results_dir}"
cp -R "${script_dir}/Assets/." "${test_project}/Assets/"
cp -R "${script_dir}/Packages" "${test_project}/Packages"
cp -R "${script_dir}/ProjectSettings" "${test_project}/ProjectSettings"
if [[ -n "${package_registry_url}" ]]; then
  command -v python3 >/dev/null 2>&1 || {
    echo "python3 is required when UNITY_PACKAGE_REGISTRY_URL is set" >&2
    exit 2
  }
  python3 -c '
import json
import sys

manifest_path = sys.argv[1]
with open(manifest_path, encoding="utf-8") as manifest_file:
    manifest = json.load(manifest_file)
manifest["scopedRegistries"] = [{
    "name": "Unity package registry override",
    "url": sys.argv[2],
    "scopes": ["com.unity"],
}]
with open(manifest_path, "w", encoding="utf-8") as manifest_file:
    json.dump(manifest, manifest_file, indent=2)
    manifest_file.write("\n")
' "${test_project}/Packages/manifest.json" "${package_registry_url}"
fi
cp -R "${sdk_root}/Facebook.Unity" "${test_project}/Assets/FacebookSDKSource/Facebook.Unity"
cp -R "${sdk_root}/Facebook.Unity.Settings" "${test_project}/Assets/FacebookSDKSource/Facebook.Unity.Settings"
mkdir -p "${test_project}/Assets/Editor/FacebookSDKEditorSource"
cp -R "${sdk_root}/Facebook.Unity.Editor/." \
  "${test_project}/Assets/Editor/FacebookSDKEditorSource/"
rm "${test_project}/Assets/FacebookSDKSource/Facebook.Unity/Properties/AssemblyInfo.cs"
rm "${test_project}/Assets/Editor/FacebookSDKEditorSource/Properties/AssemblyInfo.cs"

mkdir -p "${test_project}/Assets/FacebookSDK/Plugins/Editor"
cp "${sdk_root}/UnitySDK/Assets/FacebookSDK/Plugins/Editor/Dependencies.xml" \
  "${test_project}/Assets/FacebookSDK/Plugins/Editor/Dependencies.xml"

mono_root="$(dirname "${unity_path}")/Data/MonoBleedingEdge"
mcs_path="${mono_root}/bin/mcs"
mono_path="${mono_root}/bin/mono"
if [[ ! -x "${mcs_path}" || ! -x "${mono_path}" ]]; then
  echo "Unity bundled Mono compiler/runtime not found under: ${mono_root}" >&2
  exit 2
fi

"${mcs_path}" \
  -sdk:2 \
  -warn:4 \
  -warnaserror \
  -r:System.Xml.dll \
  -out:"${results_dir}/legacy-dotnet20-smoke.exe" \
  "${script_dir}/LegacyCompatibility/LegacyBclSmoke.cs"
"${mono_path}" "${results_dir}/legacy-dotnet20-smoke.exe" \
  > "${results_dir}/legacy-dotnet20-smoke.log"

"${unity_path}" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "${test_project}" \
  -logFile "${results_dir}/project-import.log"

set +e
"${unity_path}" \
  -batchmode \
  -nographics \
  -projectPath "${test_project}" \
  -executeMethod FacebookSdkUnity7ProjectAuditorCI.AuditAndExit \
  -logFile "${results_dir}/project-auditor.log"
audit_status=$?
set -e

if [[ -f "${test_project}/facebook-sdk-unity7-coreclr.tsv" ]]; then
  cp "${test_project}/facebook-sdk-unity7-coreclr.tsv" \
    "${results_dir}/facebook-sdk-unity7-coreclr.tsv"
fi
if [[ ${audit_status} -ne 0 ]]; then
  exit "${audit_status}"
fi
echo "Unity 7 compatibility suite passed. Results: ${results_dir}"
