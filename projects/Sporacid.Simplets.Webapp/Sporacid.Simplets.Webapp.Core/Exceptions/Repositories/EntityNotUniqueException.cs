﻿namespace Sporacid.Simplets.Webapp.Core.Exceptions.Repositories
{
    using System;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [Serializable]
    public class EntityNotUniqueException : RepositoryException
    {
        public EntityNotUniqueException()
            : base("The entity is not unique. Where clause must point to a unique entity.")
        {
        }
    }
}