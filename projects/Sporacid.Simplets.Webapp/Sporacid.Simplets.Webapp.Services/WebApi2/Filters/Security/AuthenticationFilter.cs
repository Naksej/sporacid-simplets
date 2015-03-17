﻿namespace Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.Filters;
    using Ninject;
    using Sporacid.Simplets.Webapp.Core.Exceptions.Security;
    using Sporacid.Simplets.Webapp.Core.Security.Authentication;
    using Sporacid.Simplets.Webapp.Services.Resources.Exceptions;
    using Sporacid.Simplets.Webapp.Services.Services.Security.Administration;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security.Credentials;
    using IAuthenticationModule = Sporacid.Simplets.Webapp.Core.Security.Authentication.IAuthenticationModule;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    public class AuthenticationFilter : IAuthenticationFilter
    {
        private readonly IKernel kernel;

        public AuthenticationFilter(IKernel kernel)
        {
            this.kernel = kernel;
        }

        /// <summary>
        /// Gets or sets a value indicating whether more than one instance of the indicated attribute can be specified for a single
        /// program element.
        /// </summary>
        /// <returns>
        /// true if more than one instance is allowed to be specified; otherwise, false. The default is false.
        /// </returns>
        public bool AllowMultiple
        {
            get { return false; }
        }

        /// <summary>
        /// Authenticates the request.
        /// </summary>
        /// <returns>
        /// A Task that will perform authentication.
        /// </returns>
        /// <param name="context">The authentication context.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            // Look for credentials in the request.
            var request = context.Request;
            var authorization = request.Headers.Authorization;

            // Get the culture header if it exists.
            var cultureHeader = request.Headers.AcceptLanguage.FirstOrDefault();
            if (cultureHeader != null)
            {
                // Set specific culture requested by client.
                var cultureInfo = CultureInfo.CreateSpecificCulture(cultureHeader.Value);
                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
            }


            // If there are no credentials, throw.
            if (authorization == null)
            {
                throw new SecurityException(ExceptionStrings.Services_Security_AuthHeaderRequired);
            }

            // If there are credentials but the filter does not recognize the 
            // authentication scheme, do nothing.
            AuthenticationScheme scheme;
            if (!Enum.TryParse(authorization.Scheme, out scheme))
            {
                throw new SecurityException(String.Format(ExceptionStrings.Services_Security_UnsupportedScheme, authorization.Scheme));
            }

            var authenticationModule = this.kernel.Get<IEnumerable<IAuthenticationModule>>().FirstOrDefault(a => a.IsSupported(scheme));
            if (authenticationModule == null)
            {
                throw new SecurityException(String.Format(ExceptionStrings.Services_Security_UnsupportedScheme, scheme));
            }

            var credentialsExtractor = this.kernel.Get<IEnumerable<ICredentialsExtractor>>().FirstOrDefault(e => e.IsSupported(scheme));
            if (credentialsExtractor == null)
            {
                throw new SecurityException(String.Format(ExceptionStrings.Services_Security_CannotExtractScheme, scheme));
            }

            var credentials = credentialsExtractor.Extract(authorization.Parameter);
            if (credentials == null)
            {
                throw new SecurityException(ExceptionStrings.Services_Security_InvalidCredentialsFormat);
            }

            // Authenticate the principal.
            var tokenAndPrincipal = authenticationModule.Authenticate(credentials);

            // Make sure this stupid principal is set everywhere.
            HttpContext.Current.User = Thread.CurrentPrincipal = context.Principal = tokenAndPrincipal.Principal;

            // Add token authorization informations on the response.
            var response = HttpContext.Current.Response;
            var base64Token = Convert.ToBase64String(Encoding.ASCII.GetBytes(tokenAndPrincipal.Token.Key));
            response.Headers.Add("Authorization-Token", base64Token);
            response.Headers.Add("Authorization-Token-Emitted-At", tokenAndPrincipal.Token.EmittedAt.ToString("u"));
            response.Headers.Add("Authorization-Token-Expires-At", tokenAndPrincipal.Token.EmittedAt.Add(tokenAndPrincipal.Token.ValidFor).ToString("u"));

            // Check if the user is logged in for the first time.
            var identity = tokenAndPrincipal.Principal.Identity.Name;
            var principalAdministrationService = this.kernel.Get<IPrincipalAdministrationService>();
            if (!principalAdministrationService.Exists(identity))
            {
                // User logged in for first time. Create its principal.
                principalAdministrationService.Create(identity);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            var challenge = new AuthenticationHeaderValue(AuthenticationScheme.Kerberos.ToString());
            context.Result = new AddChallengeOnUnauthorizedResult(challenge, context.Result);
            return Task.FromResult(0);
        }

        /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
        /// <version>1.9.0</version>
        private class AddChallengeOnUnauthorizedResult : IHttpActionResult
        {
            public AddChallengeOnUnauthorizedResult(AuthenticationHeaderValue challenge, IHttpActionResult innerResult)
            {
                this.Challenge = challenge;
                this.InnerResult = innerResult;
            }

            private AuthenticationHeaderValue Challenge { get; set; }
            private IHttpActionResult InnerResult { get; set; }

            public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = await this.InnerResult.ExecuteAsync(cancellationToken);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    return response;
                }

                // Only add one challenge per authentication scheme.
                if (response.Headers.WwwAuthenticate.All(h => h.Scheme != this.Challenge.Scheme))
                {
                    response.Headers.WwwAuthenticate.Add(this.Challenge);
                }

                return response;
            }
        }
    }
}