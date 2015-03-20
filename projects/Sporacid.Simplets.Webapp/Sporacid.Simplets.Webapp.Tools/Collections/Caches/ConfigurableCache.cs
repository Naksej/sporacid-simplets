﻿namespace Sporacid.Simplets.Webapp.Tools.Collections.Caches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [Serializable]
    public class ConfigurableCache<TKey, TValue> : IConfigurableCache<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        private readonly List<ICachePolicy<TKey, TValue>> registeredPolicies = new List<ICachePolicy<TKey, TValue>>();

        public ConfigurableCache(IEnumerable<ICachePolicy<TKey, TValue>> policies/*, IDictionary<TKey, TValue> baseDictionary*/)
        {
            this.dictionary = new Dictionary<TKey, TValue>();
            this.RegisterPolicies(policies.ToArray());
        }

        /// <summary>
        /// Returns whether an object is cached for the given key.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <returns>Whether an object is cached for the given key.</returns>
        public bool Has(TKey key)
        {
            // Trigger policies to take action before Has().
            this.registeredPolicies.ForEach(p => p.BeforeHas(key));

            bool has;
            try
            {
                has = this.dictionary.ContainsKey(key);
            }
            finally
            {
                // Trigger policies to take action after Has().
                this.registeredPolicies.ForEachDesc(p => p.AfterHas(key));
            }

            return has;
        }

        /// <summary>
        /// Cache an object into the cache.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <param name="value">The object to cache.</param>
        public void Put(TKey key, TValue value)
        {
            // Trigger policies to take action before Put().
            this.registeredPolicies.ForEach(p => p.BeforePut(key, value));

            try
            {
                if (this.dictionary.ContainsKey(key))
                {
                    throw new InvalidOperationException("Cache key already exists.");
                }

                this.dictionary.Add(key, value);
            }
            finally
            {
                // Trigger policies to take action after Put().
                this.registeredPolicies.ForEachDesc(p => p.AfterPut(key, value));
            }
        }

        /// <summary>
        /// Takes an action on a cached value.
        /// An exclusive lock will be acquired while the action is taken.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <param name="do">The action to take</param>
        public void WithValueDo(TKey key, Action<TValue> @do)
        {
            // Trigger policies to take action before WithValueDo().
            this.registeredPolicies.ForEach(p => p.BeforeWithValueDo(key, @do));

            try
            {
                TValue value;
                if (!this.dictionary.TryGetValue(key, out value))
                {
                    throw new InvalidOperationException("Couldn't retrieve cached value for key.");
                }

                @do(value);
            }
            finally
            {
                // Trigger policies to take action before WithValueDo().
                this.registeredPolicies.ForEachDesc(p => p.AfterWithValueDo(key, @do));
            }
        }

        /// <summary>
        /// Takes an action on a cached value.
        /// An exclusive lock will be acquired while the action is taken.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <param name="do">The action to take.</param>
        public bool TryWithValueDo(TKey key, Action<TValue> @do)
        {
            // Trigger policies to take action before WithValueDo().
            this.registeredPolicies.ForEach(p => p.BeforeWithValueDo(key, @do));
            var worked = false;

            try
            {
                TValue value;
                if (this.dictionary.TryGetValue(key, out value))
                {
                    @do(value);
                    worked = true;
                }
            }
            finally
            {
                // Trigger policies to take action before WithValueDo().
                this.registeredPolicies.ForEachDesc(p => p.AfterWithValueDo(key, @do));
            }

            return worked;
        }

        /// <summary>
        /// Takes an action on a cached value.
        /// An exclusive lock will be acquired while the action is taken.
        /// </summary>
        /// <param name="do">The action to take.</param>
        public void ForEachKeyValueDo(Action<TKey, TValue> @do)
        {
            this.dictionary.ForEach(kvp =>
            {
                var key = kvp.Key;
                this.WithValueDo(key, value => @do(key, value));
            });
        }

        /// <summary>
        /// Remove the cached object for the given key.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <returns>Whether the removal was successful.</returns>
        public bool Remove(TKey key)
        {
            // Trigger policies to take action before Remove().
            this.registeredPolicies.ForEach(p => p.BeforeRemove(key));

            bool successful;
            try
            {
                successful = this.dictionary.ContainsKey(key) && this.dictionary.Remove(key);
            }
            finally
            {
                // Trigger policies to take action after Remove().
                this.registeredPolicies.ForEachDesc(p => p.AfterRemove(key));
            }

            return successful;
        }

        /// <summary>
        /// Register a policies in the cache. Because policies have very different behaviour, caches implementation
        /// are responsible of using the policy.
        /// </summary>
        /// <param name="policies">The policies to register.</param>
        public void RegisterPolicies(params ICachePolicy<TKey, TValue>[] policies)
        {
            this.registeredPolicies.AddRange(policies);

            // Apply each policies to this cache instance.
            policies.ForEach(p => p.ApplyOn(this));
        }
    }
}