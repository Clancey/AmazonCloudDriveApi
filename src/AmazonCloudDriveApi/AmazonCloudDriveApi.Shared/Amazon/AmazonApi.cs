using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Amazon
{
	public class AmazonApi
	{
		protected string ClientSecret;

		protected string ClientId;
		public AmazonApi (string clientId, string clientSecret,HttpMessageHandler handler = null)
		{
			this.ClientId = clientId;
			this.ClientSecret = clientSecret;
			Client = handler == null ? new HttpClient () : new HttpClient (handler);
			#if __IOS__
			AmazonApi.ShowAuthenticator = (authenticator) => {
				var vc = new Amazon.iOS.AmazonWebAuthenticator (authenticator);
				var window = UIKit.UIApplication.SharedApplication.KeyWindow;
				var root = window.RootViewController;
				if(root != null)
					root.PresentViewControllerAsync(new UIKit.UINavigationController(vc),true);
			};
			#endif
		}

		protected HttpClient Client;

		Account currentAccount;

		public Account CurrentAccount {
			get {
				return currentAccount;
			}
			private set {
				currentAccount = value;
				PrepareClient (Client);
				OnAccountUpdated (currentAccount);
			}
		}

		Task<Account> authenticateTask;

		public async Task<Account> Authenticate (string identifer, string[] scope)
		{
			if (authenticateTask != null && !authenticateTask.IsCompleted) {
				return await authenticateTask;
			}
			authenticateTask = PerformAuthenticate (identifer, scope);
			return await authenticateTask;
		}

		public static Action<AmazonAuthenticator> ShowAuthenticator { get; set; }

		protected async Task<Account> PerformAuthenticate (string identifer, string[] scope)
		{
				
			var account = GetAccount (identifer);
			if (account != null) {
				var valid = account.IsValid ();
				if (!account.IsValid ()) {
					await RefreshToken (account);
				}

				if (account.IsValid ()) {
					SaveAccount (account);
					CurrentAccount = account;
					return account;
				}
			}
			var authenticator = new AmazonAuthenticator {
				ClientId = ClientId,
				Scope = scope.ToList (),
			};
			ShowAuthenticator (authenticator);

			var token = await authenticator.GetToken ();
			if (string.IsNullOrEmpty (token)) {
				throw new Exception ("Null token");
			}
			var postContent = string.Format ("grant_type=authorization_code&code={0}&client_id={1}&client_secret={2}&redirect_uri={3}", token, ClientId, ClientSecret, authenticator.RedirectUrl.AbsoluteUri);
			var reply = await Client.PostAsync ("https://api.amazon.com/auth/o2/token", new StringContent (postContent, Encoding.Default, "application/x-www-form-urlencoded"));
			var resp = await reply.Content.ReadAsStringAsync ();
			var result = Deserialize<OauthResponse> (resp);
			if (!string.IsNullOrEmpty (result.Error))
				throw new Exception (result.ErrorDescription);

			account = new Account {
				Identifier = identifer,
				ExpiresIn = result.ExpiresIn,
				Created = DateTime.UtcNow,
				RefreshToken = result.RefreshToken,
				Scope = scope,
				TokenType = result.TokenType,
				Token = result.AccessToken,
				ClientId = ClientId,
			};
			SaveAccount (account);
			CurrentAccount = account;
			return account;
		}

		protected async Task RefreshToken (Account account)
		{
			try {
				var postContent = string.Format ("grant_type=refresh_token&refresh_token={0}&client_id={1}&client_secret={2}", account.RefreshToken, ClientId, ClientSecret);
				var reply = await Client.PostAsync ("https://api.amazon.com/auth/o2/token", new StringContent (postContent, Encoding.Default, "application/x-www-form-urlencoded"));
				var resp = await reply.Content.ReadAsStringAsync ();
				var result = Deserialize<OauthResponse> (resp);
				if (!string.IsNullOrEmpty (result.Error))
					throw new Exception (result.ErrorDescription);
				account.RefreshToken = result.RefreshToken;
				account.TokenType = result.TokenType;
				account.Token = result.AccessToken;
				account.ExpiresIn = result.ExpiresIn;
				account.Created = DateTime.UtcNow;
				if (account == CurrentAccount)
					OnAccountUpdated (account);
				SaveAccount (account);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		protected virtual async Task OnAccountUpdated (Account account)
		{

		}

		protected virtual void SaveAccount (Account account)
		{
			Utility.SetSecured (account.Identifier, SerializeObject (account),ClientSecret);
		}

		protected virtual Account GetAccount (string identifier)
		{
			try {
				var data = Utility.GetSecured (identifier,ClientSecret);
				return string.IsNullOrWhiteSpace (data) ? null : Deserialize<Account> (data);
			} catch (Exception ex) {
				return null;
			}
		}


		public async virtual Task<List<T>> GetGenericList<T> (string path)
		{
			var items = await Get<List<T>> (path);

			return items;
		}

		public async Task PrepareClient (HttpClient client)
		{
			if (CurrentAccount == null)
				throw new Exception ("Not Authenticated");
			if (!CurrentAccount.IsValid ())
				await RefreshToken (CurrentAccount);
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue (currentAccount.TokenType, currentAccount.Token);

		}

		public async virtual Task<Stream> GetUrlStream (string path)
		{
//			var resp = await GetMessage (path);
//			return await resp.Content.ReadAsStreamAsync ();
			return await Client.GetStreamAsync (new Uri (path));
		}


		public async virtual Task<string> GetString (string path)
		{
			var resp = await GetMessage (path);
			return await resp.Content.ReadAsStringAsync ();
		}

		public virtual async Task<string> PostUrl (string path, string content, string mediaType = "text/json")
		{
			var message = await Client.PostAsync (path, new StringContent (content, System.Text.Encoding.UTF8, mediaType));
			return await message.Content.ReadAsStringAsync ();
		}


		public virtual async Task<T> Get<T> (string path, string id = "")
		{
			var data = await GetString (Path.Combine (path, id));
			return await Task.Factory.StartNew (() => {
				return Deserialize<T> (data);
			});
			
		}

		public virtual async Task<T> Post<T> (string path, string content)
		{
			Console.WriteLine ("{0} - {1}", path, content);
			var data = await PostUrl (path, content);
			Console.WriteLine (data);
			return await Task.Factory.StartNew (() => {
				return Deserialize<T> (data);
			});
			
		}

		public async Task<HttpResponseMessage> PostMessage (string path, HttpContent content)
		{
			if (CurrentAccount == null)
				throw new Exception ("Not Authenticated");
			if (!CurrentAccount.IsValid ())
				await RefreshToken (CurrentAccount);
			return await Client.PostAsync (path, content);
		}

		public async Task<HttpResponseMessage> PutMessage (string path, HttpContent content)
		{
			if (CurrentAccount == null)
				throw new Exception ("Not Authenticated");
			if (!CurrentAccount.IsValid ())
				await RefreshToken (CurrentAccount);
			return await Client.PutAsync (path, content);
		}


		public async Task<HttpResponseMessage> GetMessage (string path)
		{
			if (CurrentAccount == null)
				throw new Exception ("Not Authenticated");
			if (!CurrentAccount.IsValid ())
				await RefreshToken (CurrentAccount);
			return await Client.GetAsync (path);
		}

		protected virtual T Deserialize<T> (string data)
		{
			try {
				return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (data);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			return default(T);
		}

		protected virtual string SerializeObject (object obj)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject (obj);
		}
	}
}

