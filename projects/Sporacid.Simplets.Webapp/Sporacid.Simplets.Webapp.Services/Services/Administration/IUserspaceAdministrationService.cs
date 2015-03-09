﻿namespace Sporacid.Simplets.Webapp.Services.Services.Administration
{
    using System;
    using System.Diagnostics.Contracts;
    using Sporacid.Simplets.Webapp.Core.Exceptions;
    using Sporacid.Simplets.Webapp.Core.Exceptions.Repositories;
    using Sporacid.Simplets.Webapp.Core.Exceptions.Security.Authorization;
    using Sporacid.Simplets.Webapp.Core.Security.Authorization;
    using Sporacid.Simplets.Webapp.Services.Resources.Contracts;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [Module("Administration")]
    [FixedContext(SecurityConfig.SystemContext)]
    [ContractClass(typeof (UserspaceAdministrationServiceContract))]
    public interface IUserspaceAdministrationService
    {
        /// <summary>
        /// Creates the base profil entity for a given principal's identity.
        /// Every available informations for the principal will be extracted and included in the profil entity.
        /// </summary>
        /// <param name="identity">The principal's identity.</param>
        /// <exception cref="NotAuthorizedException">
        /// If the profil entity already exists.
        /// </exception>
        /// <exception cref="RepositoryException">
        /// If something unexpected occurs while creating the base profil entity.
        /// </exception>
        /// <exception cref="CoreException">
        /// If something unexpected occurs.
        /// </exception>
        /// <returns>The id of the created profil entity.</returns>
        [RequiredClaims(Claims.Admin | Claims.Create)]
        Int32 CreateBaseProfil(String identity);
    }

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    [ContractClassFor(typeof (IUserspaceAdministrationService))]
    internal abstract class UserspaceAdministrationServiceContract : IUserspaceAdministrationService
    {
        /// <summary>
        /// Creates the base profil for agiven universal code.
        /// </summary>
        /// <param name="codeUniversel">The universal code that represents the profil entity.</param>
        /// <returns>The id of the newly created profil entity.</returns>
        public Int32 CreateBaseProfil(String codeUniversel)
        {
            // Preconditions.
            Contract.Requires(!String.IsNullOrEmpty(codeUniversel), ContractStrings.UserspaceAdministrationService_CreateBaseProfil_RequiresCodeUniversel);

            // Postconditions.
            Contract.Ensures(Contract.Result<Int32>() > 0, ContractStrings.UserspaceAdministrationService_CreateBaseProfil_EnsuresPositiveProfilId);

            // Dummy return.
            return default(Int32);
        }
    }
}