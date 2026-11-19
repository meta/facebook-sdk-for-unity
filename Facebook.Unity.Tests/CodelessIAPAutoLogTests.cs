/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace UnityEngine.Purchasing
{
    // Stand-in for Unity IAP's Product, found by CodelessIAPAutoLog through reflection.
    public class Product
    {
    }
}

namespace Facebook.Unity.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine.Purchasing;

    [TestFixture]
    public class CodelessIAPAutoLogTests
    {
        [Test]
        public void AddsListenerToProductEvent()
        {
            object purchaseEvent = CreateUnityEvent(typeof(Product));
            var listener = new PurchaseListener();
            CodelessIAPAutoLog.addListenerToGameObject(new FakeIAPButton { onPurchaseComplete = purchaseEvent }, listener);
            CodelessIAPAutoLog.addListenerToGameObject(new FakeIAPButton { onPurchaseComplete = purchaseEvent }, listener);

            Raise(purchaseEvent, new Product());

            Assert.AreEqual(1, listener.Calls);
        }

        [Test]
        public void IgnoresMissingPurchaseEvent()
        {
            var listener = new PurchaseListener();
            Assert.DoesNotThrow(() => CodelessIAPAutoLog.addListenerToGameObject(new FakeIAPButton(), listener));
            Assert.AreEqual(0, listener.Calls);
        }

        [Test]
        public void IgnoresPurchaseEventOfAnotherType()
        {
            object purchaseEvent = CreateUnityEvent(typeof(string));
            var listener = new PurchaseListener();
            Assert.DoesNotThrow(() => CodelessIAPAutoLog.addListenerToGameObject(new FakeIAPButton { onPurchaseComplete = purchaseEvent }, listener));

            Raise(purchaseEvent, "order");

            Assert.AreEqual(0, listener.Calls);
        }

        // The test project references both UnityEngine.dll and UnityEngine.CoreModule.dll, so UnityEvent<T>
        // is ambiguous at compile time. Use the CoreModule type, which is the one the SDK binds to.
        private static object CreateUnityEvent(Type argumentType)
        {
            Type unityEvent = AppDomain.CurrentDomain.GetAssemblies()
                .First(assembly => assembly.GetName().Name == "UnityEngine.CoreModule")
                .GetType("UnityEngine.Events.UnityEvent`1", true);
            return Activator.CreateInstance(unityEvent.MakeGenericType(argumentType));
        }

        private static void Raise(object unityEvent, object argument)
        {
            unityEvent.GetType().GetMethod("Invoke").Invoke(unityEvent, new[] { argument });
        }

        public class FakeIAPButton
        {
            public object onPurchaseComplete;
        }

        public class PurchaseListener
        {
            public int Calls { get; private set; }

            public void onPurchaseCompleteHandler(object data)
            {
                this.Calls++;
            }
        }
    }
}
