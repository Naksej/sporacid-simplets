﻿namespace Sporacid.Simplets.Webapp.Tools.Collections.Caches
{
    using System;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface ICache<TKey, TValue>
    {
        /// <summary>
        /// Returns whether an object is cached for the given key.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <returns>Whether an object is cached for the given key.</returns>
        bool Has(TKey key);

        /// <summary>
        /// Cache an object into the cache.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <param name="value">The object to cache.</param>
        void Put(TKey key, TValue value);

        /// <summary>
        /// Takes an action on a cached value.
        /// An exclusive lock will be acquired while the action is taken.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <param name="do">The action to take.</param>
        void WithValueDo(TKey key, Action<TValue> @do);

        /// <summary>
        /// Takes an action on a cached value.
        /// An exclusive lock will be acquired while the action is taken.
        /// </summary>
        /// <param name="do">The action to take.</param>
        void ForEachKeyValueDo(Action<TKey, TValue> @do);

        /// <summary>
        /// Remove the cached object for the given key.
        /// </summary>
        /// <param name="key">The key object.</param>
        /// <returns>Whether the removal was successful.</returns>
        bool Remove(TKey key);
    }
}