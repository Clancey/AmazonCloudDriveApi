﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Amazon
{
    public abstract class Authenticator
    {
	    public abstract string BaseUrl { get; }
	    public abstract Uri RedirectUrl { get; }

	    public string AuthCode { get; private set; }

	    public Authenticator()
		{
			AllowsCancel = true;
			Title = "Sign in";
			Scope = new List<string>();
		}

		public string Title { get; set; }

		TaskCompletionSource<string> tokenTask;

		public async Task<string> GetAuthCode()
		{
			if (tokenTask != null && !tokenTask.Task.IsCompleted)
			{
				return await tokenTask.Task;
			}
			tokenTask = new TaskCompletionSource<string>();
			return await tokenTask.Task;
		}

		public bool AllowsCancel { get; set; }

		public void OnCancelled()
		{
			HasCompleted = true;
			tokenTask.TrySetCanceled();
		}

		public virtual void CheckUrl(Uri url, Cookie[] cookies)
		{
			try
			{
				if (url == null || string.IsNullOrWhiteSpace(url.Query))
					return;
				if (url.Host != RedirectUrl.Host)
					return;
				var parts = HttpUtility.ParseQueryString(url.Query);
				var code = parts["code"];
				if (!string.IsNullOrWhiteSpace(code) && tokenTask != null)
					FoundAuthCode(code);

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

	    protected void FoundAuthCode(string authCode)
	    {
			HasCompleted = !string.IsNullOrWhiteSpace(authCode);
			AuthCode = authCode;
            tokenTask.TrySetResult(authCode);
		}

	    public void OnError(string error)
		{
			tokenTask.TrySetException(new Exception(error));
		}
		public string ClientId { get; set; }
		public List<string> Scope { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public virtual async Task<Uri> GetInitialUrl()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			var scope = string.Join("%20", Scope.Select(HttpUtility.UrlEncode));
			var url = $"{BaseUrl}client_id={ClientId}&scope={scope}&response_type=code&redirect_uri={RedirectUrl.AbsoluteUri}";
			return new Uri(url);
		}

#pragma warning disable 1998
	    public virtual async Task<Dictionary<string, string>> GetTokenPostData(string clientSecret)
#pragma warning restore 1998
	    {
			return new Dictionary<string,string>
		    {
			    {"grant_type","authorization_code"},
				{"code",AuthCode},
				{"client_id",ClientId},
				{"client_secret",clientSecret},
		    };
	    }

	    public bool ClearCookiesBeforeLogin { get; set; }

		public bool HasCompleted { get; private set; }

	}
	
}
