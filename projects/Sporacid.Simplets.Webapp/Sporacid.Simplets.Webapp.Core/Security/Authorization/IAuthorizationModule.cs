﻿namespace Sporacid.Simplets.Webapp.Core.Security.Authorization
{
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;
    using PostSharp.Patterns.Contracts;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    public interface IAuthorizationModule
    {
        /// <summary>
        /// Authorizes an authenticated principal for the given module and contexts. If the user is not what
        /// he claims to be, an authorization exception will be raised.
        /// </summary>
        /// <param name="principal">The principal of an authenticated user.</param>
        /// <param name="claims">What the user claims to be authorized to do on the module and context.</param>
        /// <param name="module">The module name which the user tries to access.</param>
        /// <param name="contexts">The context name which the user tries to access.</param>
        void Authorize([Required] IPrincipal principal, [Required] Claims claims, [Required] String module, [NotEmpty] params String[] contexts);
    }
}