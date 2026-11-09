/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Facebook.Unity
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Events;
    using System.Collections.Generic;

    internal class CodelessIAPAutoLog
    {
        internal static void handlePurchaseCompleted(System.Object data)
        {
            try
            {
                if (!FB.Mobile.IsImplicitPurchaseLoggingEnabled())
                {
                    return;
                }
                object metadata = CodelessIAPAutoLog.GetProperty(data, "metadata");
                object productDefinition = CodelessIAPAutoLog.GetProperty(data, "definition");
                if (metadata == null || productDefinition == null)
                {
                    return;
                }
                Decimal price = (Decimal)CodelessIAPAutoLog.GetProperty(metadata, "localizedPrice");
                String currency = (String)CodelessIAPAutoLog.GetProperty(metadata, "isoCurrencyCode");
                String id = (String)CodelessIAPAutoLog.GetProperty(productDefinition, "id");
                FB.LogAppEvent(
                    AppEventName.Purchased,
                    (float)price,
                    new Dictionary<string, object>()
                    {
                        { "_implicitlyLogged", "1" },
                        { AppEventParameterName.Currency, currency },
                        { AppEventParameterName.ContentID, id }
                    });
            }
            catch (System.Exception e)
            {
                FacebookLogger.Log("Failed to automatically handle Purchase Completed: " + e.Message);
            }
        }

        internal static void addListenerToIAPButtons(object listenerObject)
        {
            UnityEngine.Object[] iapButtons = FindObjectsOfTypeByName("IAPButton", "UnityEngine.Purchasing");
            if (iapButtons == null)
            {
                return;
            }
            foreach (UnityEngine.Object btn in iapButtons)
            {
                addListenerToGameObject(btn, listenerObject);
            }
        }

        internal static void addListenerToGameObject(UnityEngine.Object gameObject, object listenerObject)
        {
            // Code stripping will work here when developer uses Unity IAP service the UnityEngine.Purchasing will be linked
            // If it's not enabled, this will return null and it's expected and handled in null check
            Type productType = FindTypeInAssemblies("Product", "UnityEngine.Purchasing");
            if (productType == null)
            {
                return;
            }
            Type eventGeneric = typeof(UnityEvent<>);
            Type actionGeneric = typeof(UnityAction<>);

            Type[] typeArgs = { productType };
            Type unityEventProduct = eventGeneric.MakeGenericType(typeArgs);
            Type unityActionProduct = actionGeneric.MakeGenericType(typeArgs);

            UnityEventBase onPurchaseComplete = (UnityEventBase)GetField(gameObject, "onPurchaseComplete");
            MethodInfo addListener = unityEventProduct.GetMethod("AddListener");
            MethodInfo removeListener = unityEventProduct.GetMethod("RemoveListener");

            MethodInfo method = listenerObject.GetType().GetMethod(
                "onPurchaseCompleteHandler",
                BindingFlags.Instance | BindingFlags.Public
            );

            removeListener.Invoke(onPurchaseComplete, new object[] {
                System.Delegate.CreateDelegate(unityActionProduct, listenerObject, method)
            });
            addListener.Invoke(onPurchaseComplete, new object[] {
                System.Delegate.CreateDelegate(unityActionProduct, listenerObject, method)
            });
        }

        private static System.Type FindTypeInAssemblies(string typeName, string nameSpace)
        {
            var assemblies = GetLoadedAssemblies();
            var fullTypeName = nameSpace + "." + typeName;
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(fullTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static IEnumerable<Assembly> GetLoadedAssemblies()
        {
            var currentAssembliesType = typeof(UnityEngine.Object).Assembly.GetType(
                "UnityEngine.Assemblies.CurrentAssemblies",
                false);
            var getLoadedAssemblies = currentAssembliesType == null
                ? null
                : currentAssembliesType.GetMethod(
                    "GetLoadedAssemblies",
                    BindingFlags.Public | BindingFlags.Static);
            if (getLoadedAssemblies != null)
            {
                try
                {
                    var loadedAssemblies = getLoadedAssemblies.Invoke(null, null) as IEnumerable<Assembly>;
                    if (loadedAssemblies != null)
                    {
                        return loadedAssemblies;
                    }
                }
                catch (Exception exception)
                {
                    FacebookLogger.Warn(
                        "Unable to enumerate loaded assemblies through CurrentAssemblies: " +
                        exception.Message);
                }
            }

            var getAssemblies = typeof(AppDomain).GetMethod(
                "GetAssemblies",
                BindingFlags.Public | BindingFlags.Instance);
            IEnumerable<Assembly> appDomainAssemblies = null;
            if (getAssemblies != null)
            {
                try
                {
                    appDomainAssemblies = getAssemblies.Invoke(
                        AppDomain.CurrentDomain,
                        null) as IEnumerable<Assembly>;
                }
                catch (Exception exception)
                {
                    FacebookLogger.Warn(
                        "Unable to enumerate loaded assemblies through AppDomain: " +
                        exception.Message);
                }
            }

            if (appDomainAssemblies == null)
            {
                FacebookLogger.Warn("Unable to enumerate loaded assemblies on this Unity runtime");
                return new Assembly[0];
            }

            return appDomainAssemblies;
        }

        private static UnityEngine.Object[] FindObjectsOfTypeByName(string typeName, string nameSpace)
        {
            // Code stripping will work here when developer uses Unity IAP service the UnityEngine.Purchasing will be linked
            // If it's not enabled, this will return null and it's expected and handled in null check
            Type type = FindTypeInAssemblies(typeName, nameSpace);
            if (type == null)
            {
                return null;
            }
            var oneArgumentFind = typeof(UnityEngine.Object).GetMethod(
                "FindObjectsByType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Type) },
                null);
            if (oneArgumentFind != null)
            {
                return oneArgumentFind.Invoke(null, new object[] { type }) as UnityEngine.Object[];
            }

            // Match the pre-Unity 7 API exactly. Unity exposes other overloads
            // whose second parameter is an unrelated enum.
            var sortModeType = typeof(UnityEngine.Object).Assembly.GetType(
                "UnityEngine.FindObjectsSortMode",
                false);
            var twoArgumentFind = sortModeType == null
                ? null
                : typeof(UnityEngine.Object).GetMethod(
                    "FindObjectsByType",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Type), sortModeType },
                    null);
            if (twoArgumentFind != null && sortModeType.IsEnum &&
                Enum.IsDefined(sortModeType, "None"))
            {
                var unsorted = Enum.Parse(sortModeType, "None");
                return twoArgumentFind.Invoke(
                    null,
                    new object[] { type, unsorted }) as UnityEngine.Object[];
            }

            var legacyFind = typeof(UnityEngine.Object).GetMethod(
                "FindObjectsOfType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Type) },
                null);
            if (legacyFind == null)
            {
                FacebookLogger.Warn("Unable to find a supported Unity object lookup API");
                return null;
            }

            return legacyFind.Invoke(null, new object[] { type }) as UnityEngine.Object[];
        }

        private static object GetField(object inObj, string fieldName)
        {
            object ret = null;
            FieldInfo info = inObj.GetType().GetField(fieldName);
            if (info != null)
                ret = info.GetValue(inObj);
            return ret;
        }

        private static object GetProperty(object inObj, string propertyName)
        {
            object ret = null;
            PropertyInfo info = inObj.GetType().GetProperty(propertyName);
            if (info != null)
                ret = info.GetValue(inObj, null);
            return ret;
        }
    }
}
