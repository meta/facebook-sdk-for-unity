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
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    /// <summary>
    /// Contains the access token and related information.
    /// </summary>
    public partial class AccessToken
    {
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static AccessToken currentAccessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessToken"/> class.
        /// </summary>
        /// <param name="tokenString">Token string of the token.</param>
        /// <param name="userId">User identifier of the token.</param>
        /// <param name="expirationTime">Expiration time of the token.</param>
        /// <param name="permissions">Permissions of the token.</param>
        /// <param name="lastRefresh">Last Refresh time of token.</param>
        internal AccessToken(
            string tokenString,
            string userId,
            DateTime expirationTime,
            IEnumerable<string> permissions,
            DateTime? lastRefresh,
            string graphDomain)
        {
            if (string.IsNullOrEmpty(tokenString))
            {
                throw new ArgumentNullException("tokenString");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            if (expirationTime == DateTime.MinValue)
            {
                throw new ArgumentException("Expiration time is unassigned");
            }

            if (permissions == null)
            {
                throw new ArgumentNullException("permissions");
            }

            this.TokenString = tokenString;
            this.ExpirationTime = expirationTime;
            this.Permissions = permissions;
            this.UserId = userId;
            this.LastRefresh = lastRefresh;
            this.GraphDomain = graphDomain;
        }

        /// <summary>
        /// Gets the current access token.
        /// </summary>
        /// <value>The current access token.</value>
        public static AccessToken CurrentAccessToken
        {
            get { return currentAccessToken; }
            internal set { currentAccessToken = value; }
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void ResetStaticState()
        {
            CurrentAccessToken = null;
        }

        /// <summary>
        /// Gets the token string.
        /// </summary>
        /// <value>The token string.</value>
        public string TokenString { get; private set; }

        /// <summary>
        /// Gets the expiration time.
        /// </summary>
        /// <value>The expiration time.</value>
        public DateTime ExpirationTime { get; private set; }

        /// <summary>
        /// Gets the list of permissions.
        /// </summary>
        /// <value>The permissions.</value>
        public IEnumerable<string> Permissions { get; private set; }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; private set; }

        /// <summary>
        /// Gets the last refresh.
        /// </summary>
        /// <value>The last refresh.</value>
        public DateTime? LastRefresh { get; private set; }


        /// <summary>
        /// Gets the domain this access token is valid for.
        /// </summary>
        public String GraphDomain { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Facebook.Unity.AccessToken"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Facebook.Unity.AccessToken"/>.</returns>
        public override string ToString()
        {
            return Utilities.FormatToString(
                null,
                this.GetType().Name,
                new Dictionary<string, string>()
                {
                    { "ExpirationTime", this.ExpirationTime.TotalSeconds().ToString() },
                    { "Permissions", this.Permissions.ToCommaSeparateList() },
                    { "UserId", this.UserId.ToStringNullOk() },
                    { "LastRefresh", this.LastRefresh.ToStringNullOk() },
                    { "GraphDomain", this.GraphDomain },
                });
        }

        internal string ToJson()
        {
            var dictionary = new Dictionary<string, string>();
            dictionary[LoginResult.PermissionsKey] = string.Join(",", this.Permissions.ToArray());
            dictionary[LoginResult.ExpirationTimestampKey] = this.ExpirationTime.TotalSeconds().ToString();
            dictionary[LoginResult.AccessTokenKey] = this.TokenString;
            dictionary[LoginResult.UserIdKey] = this.UserId;
            if (this.LastRefresh != null)
            {
                dictionary[LoginResult.LastRefreshKey] = this.LastRefresh.Value.TotalSeconds().ToString();
            }
            dictionary[LoginResult.GraphDomain] = this.GraphDomain;


            return MiniJSON.Json.Serialize(dictionary);
        }
    }
}
