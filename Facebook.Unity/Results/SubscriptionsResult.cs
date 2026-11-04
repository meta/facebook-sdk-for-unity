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
    using System.Collections.Generic;
    using System.Text;

    internal class SubscriptionsResult : ResultBase, ISubscriptionsResult
    {
        public SubscriptionsResult(ResultContainer resultContainer) : base(resultContainer)
        {
            if (this.ResultDictionary != null && this.ResultDictionary.ContainsKey("success"))
            {
                this.Subscriptions = Utilities.ParseSubscriptionsFromResult(this.ResultDictionary);
            }
            else if (this.ResultDictionary != null && this.ResultDictionary.ContainsKey("subscriptions"))
            {
                this.ResultDictionary.TryGetValue("subscriptions", out object productsList);
                this.Subscriptions = (IList<Subscription>)productsList;
            }
        }

        public IList<Subscription> Subscriptions { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var subscription in this.Subscriptions)
            {
                sb.AppendLine(subscription.ToString());
            }

            return Utilities.FormatToString(
                null,
                this.GetType().Name,
                new Dictionary<string, string>()
                {
                    { "Subscriptions", sb.ToString() },
                });
        }
    }
}
