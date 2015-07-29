using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Amazon
{

	public abstract class OAuthApi : Api
	{
		public OAuthApi(string identifier, string clientId, string clientSecret, HttpMessageHandler handler = null) : base(identifier,handler)
		{
			this.ClientId = clientId;
			this.ClientSecret = clientSecret;
#if __IOS__
			Api.ShowAuthenticator = (authenticator) =>
			{
				var invoker = new Foundation.NSObject();
				invoker.BeginInvokeOnMainThread(() =>
				{
					var vc = new iOS.WebAuthenticator(authenticator);
					var window = UIKit.UIApplication.SharedApplication.KeyWindow;
					var root = window.RootViewController;
					if (root != null)
					{
						var current = root;
						while (current.PresentedViewController != null)
						{
							current = current.PresentedViewController;
						}
						current.PresentViewControllerAsync(new UIKit.UINavigationController(vc), true);
					}
				});
			};
#endif
		}

		public OAuthAccount CurrentOAuthAccount => CurrentAccount as OAuthAccount;

		public string TokenUrl { get; set; }

		protected override async Task<Account> PerformAuthenticate(string[] scope)
		{
			var account = CurrentOAuthAccount ?? GetAccount<OAuthAccount>(Identifier);
			if (account != null && !string.IsNullOrWhiteSpace(account.RefreshToken))
			{
				var valid = account.IsValid();
				if (!valid)
				{
					await RefreshToken(account);
				}

				if (account.IsValid())
				{
					SaveAccount(account);
					CurrentAccount = account;
					return account;
				}
			}
			var authenticator = CreateAuthenticator(scope); 

			ShowAuthenticator(authenticator);

			var token = await authenticator.GetAuthCode();
			if (string.IsNullOrEmpty(token))
			{
				throw new Exception("Null token");
			}
			account = await GetAccountFromAuthCode(authenticator,Identifier);
			account.Identifier = Identifier;
			SaveAccount(account);
			CurrentAccount = account;
			return account;
		}

		protected virtual async Task<OAuthAccount> GetAccountFromAuthCode(Authenticator authenticator, string identifier)
		{
			var postData = await authenticator.GetTokenPostData(ClientSecret);
			if(string.IsNullOrWhiteSpace(TokenUrl))
				throw new Exception("Invalid TokenURL");
			var reply = await Client.PostAsync(TokenUrl, new FormUrlEncodedContent(postData));
			var resp = await reply.Content.ReadAsStringAsync();
			var result = Deserialize<OauthResponse>(resp);
			if (!string.IsNullOrEmpty(result.Error))
				throw new Exception(result.ErrorDescription);

			var account = new OAuthAccount()
			{
				ExpiresIn = result.ExpiresIn,
				Created = DateTime.UtcNow,
				RefreshToken = result.RefreshToken,
				Scope = authenticator.Scope.ToArray(),
				TokenType = result.TokenType,
				Token = result.AccessToken,
				ClientId = ClientId,
				Identifier = identifier,
			};
			return account;
		}

		protected abstract Authenticator CreateAuthenticator(string[] scope);
		protected async Task RefreshToken(Account accaccount)
		{
			try
			{
				var account = accaccount as OAuthAccount;
				if(account == null)
					throw new Exception("Invalid Account");

				var reply = await Client.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string,string>
				{
					{"grant_type","refresh_token"},
					{"refresh_token",account.RefreshToken},
					{"client_id",ClientId},
					{"client_secret",ClientSecret},
				}));
				var resp = await reply.Content.ReadAsStringAsync();
				var result = Deserialize<OauthResponse>(resp);
				if (!string.IsNullOrEmpty(result.Error))
				{
					if (string.IsNullOrWhiteSpace(account.RefreshToken) || result.Error == "invalid_grant" || result.ErrorDescription.IndexOf("revoked", StringComparison.CurrentCultureIgnoreCase) >= 0)
					{
						account.Token = "";
						account.RefreshToken = "";
						account.ExpiresIn = 1;
						SaveAccount(account);
						await Authenticate();
						return;
					}
					else
						throw new Exception(result.ErrorDescription);
				}
				if(!string.IsNullOrEmpty(result.RefreshToken))
					account.RefreshToken = result.RefreshToken;
				account.TokenType = result.TokenType;
				account.Token = result.AccessToken;
				account.ExpiresIn = result.ExpiresIn;
				account.Created = DateTime.UtcNow;
				if (account == CurrentAccount)
					await OnAccountUpdated(account);
				CurrentAccount = account;
				SaveAccount(account);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		Task refreshTask;
		protected override async Task RefreshAccount(Account account)
		{
			if (refreshTask == null || refreshTask.IsCompleted)
				refreshTask = RefreshToken(account);
			await refreshTask;
		}

		public override async Task PrepareClient(HttpClient client)
		{
			await base.PrepareClient(client);

			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(CurrentOAuthAccount.TokenType, CurrentOAuthAccount.Token);
		}
	}

	public abstract class Api
	{
		public bool Verbose { get; set; } = false;
		public string Identifier {get; private set;}

		public virtual string ExtraDataString { get; set; }

		protected string ClientSecret;

		protected string ClientId;
		
		protected HttpClient Client;

		public Api(string identifier, HttpMessageHandler handler = null)
		{
			Identifier = identifier;
			Client = handler == null ? new HttpClient() : new HttpClient(handler);
		}

		public bool HasAuthenticated { get; private set; }

		TaskCompletionSource<bool> authenticatingTask = new TaskCompletionSource<bool>(); 



		protected Account currentAccount;

		public Account CurrentAccount
		{
			get
			{
				return currentAccount;
			}
			protected set
			{
				currentAccount = value;
				HasAuthenticated = true;
#pragma warning disable 4014
				PrepareClient(Client);
				authenticatingTask.TrySetResult(true);
				OnAccountUpdated(currentAccount);
#pragma warning restore 4014
			}
		}

		Task<Account> authenticateTask;
		public async Task<Account> Authenticate(string[] scope)
		{
			if (authenticateTask != null && !authenticateTask.IsCompleted)
			{
				return await authenticateTask;
			}
			authenticateTask = PerformAuthenticate(scope);
			var result = await authenticateTask;
			if (result == null)
			{
				authenticatingTask.TrySetResult(false);
			}
			return result;
		}

		public abstract Task<Account> Authenticate();

		protected abstract Task<Account> PerformAuthenticate(string[] scope);

		protected abstract Task RefreshAccount(Account account);

		public static Action<Authenticator> ShowAuthenticator { get; set; }
		public string DeviceId { get; set; }

		protected virtual async Task OnAccountUpdated(Account account)
		{

		}

		protected virtual void SaveAccount(Account account)
		{
			Utility.SetSecured(account.Identifier, SerializeObject(account), ClientSecret);
		}

		protected virtual T GetAccount<T>(string identifier) where T : Account
		{
			try
			{
				var data = Utility.GetSecured(identifier, ClientSecret);
				return string.IsNullOrWhiteSpace(data) ? null : Deserialize<T>(data);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return null;
			}
		}


		public async virtual Task<List<T>> GetGenericList<T>(string path)
		{
			var items = await Get<List<T>>(path);

			return items;
		}

		public virtual async Task PrepareClient(HttpClient client)
		{
			await VerifyCredentials();
		}

		public async virtual Task<Stream> GetUrlStream(string path)
		{
			//			var resp = await GetMessage (path);
			//			return await resp.Content.ReadAsStreamAsync ();
			return await Client.GetStreamAsync(new Uri(path));
		}


		public async virtual Task<string> GetString(string path)
		{
			var resp = await GetMessage(path);
			return await resp.Content.ReadAsStringAsync();
		}

		public virtual async Task<string> PostUrl(string path, string content, string mediaType = "text/json")
		{
			var message = await Client.PostAsync(path, new StringContent(content, System.Text.Encoding.UTF8, mediaType));
			return await message.Content.ReadAsStringAsync();
		}


		public virtual async Task<T> Get<T>(string path, string id = "")
		{
			var data = await GetString(Path.Combine(path, id));
			return await Task.Factory.StartNew(() => Deserialize<T>(data));

		}

		public virtual async Task<T> Post<T>(string path, string content)
		{
			Debug.WriteLine("{0} - {1}", path, content);
			var data = await PostUrl(path, content);
			if(Verbose)
				Debug.WriteLine(data);
			return await Task.Factory.StartNew(() => Deserialize<T>(data));

		}
		public virtual async Task<T> Post<T>(string path, HttpContent content)
		{
			Debug.WriteLine("{0} - {1}", path, await content.ReadAsStringAsync());
			var resp = await PostMessage(path, content);
			var data = await resp.Content.ReadAsStringAsync();
			if(Verbose)
				Debug.WriteLine(data);
			return await Task.Factory.StartNew(() => Deserialize<T>(data));

		}
		public async Task<HttpResponseMessage> PostMessage(string path, HttpContent content)
		{
			await VerifyCredentials();
			return await Client.PostAsync(path, content);
		}

		public async Task<HttpResponseMessage> PutMessage(string path, HttpContent content)
		{
			await VerifyCredentials();
			return await Client.PutAsync(path, content);
		}

		public async Task<HttpResponseMessage> GetMessage(string path)
		{
			await VerifyCredentials();
            return await Client.GetAsync(path);
		}

		protected virtual async Task VerifyCredentials()
		{
			if (CurrentAccount == null)
			{
				throw new Exception("Not Authenticated");
			}
			if (!CurrentAccount.IsValid())
				await RefreshAccount(CurrentAccount);
		}

		protected virtual T Deserialize<T>(string data)
		{
			try
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			return default(T);
		}

		protected virtual string SerializeObject(object obj)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
		}
	}
}

