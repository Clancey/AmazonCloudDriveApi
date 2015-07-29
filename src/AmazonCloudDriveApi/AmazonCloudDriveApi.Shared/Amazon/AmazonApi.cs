using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Amazon
{
	public abstract class AmazonApi : OAuthApi
	{
		public AmazonApi(string identifier,string clientId, string clientSecret, HttpMessageHandler handler = null) : base(identifier,clientId, clientSecret, handler)
		{
			TokenUrl = "https://api.amazon.com/auth/o2/token";
		}

		protected override Authenticator CreateAuthenticator(string[] scope)
		{
			return new AmazonAuthenticator {
				Scope =  scope.ToList(),
				ClientId = ClientId,
			};
		}
	}
}
