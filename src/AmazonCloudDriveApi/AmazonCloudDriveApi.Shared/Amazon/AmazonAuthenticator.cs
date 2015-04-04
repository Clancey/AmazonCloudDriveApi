using System;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Generic;
using System.Linq;

namespace Amazon
{
	public class AmazonAuthenticator
	{
		public string BaseUrl = "https://www.amazon.com/ap/oa?";
		public Uri RedirectUrl = new Uri ("http://localhost");
		public AmazonAuthenticator ()
		{
			AllowsCancel = true;
			Title = "Sign in";
			Scope = new List<string> ();
		}

		public string Title { get; set; }

		TaskCompletionSource<string> tokenTask;

		public async Task<string> GetToken ()
		{
			if (tokenTask != null && !tokenTask.Task.IsCompleted) {
				return await tokenTask.Task;
			}
			tokenTask = new TaskCompletionSource<string> ();
			return await tokenTask.Task;
		}

		public bool AllowsCancel { get; set; }

		public void OnCancelled ()
		{
			tokenTask.TrySetCanceled ();
		}

		public void CheckUrl (Uri url)
		{
			try {
				if (url == null || string.IsNullOrWhiteSpace (url.Query))
					return;
				if(url.Host != RedirectUrl.Host)
					return;
				var parts = HttpUtility.ParseQueryString (url.Query);
				var code = parts["code"];
				HasCompleted = !string.IsNullOrWhiteSpace(code);
				if(!string.IsNullOrWhiteSpace(code) && tokenTask != null)
					tokenTask.TrySetResult(code);
					
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}

			
		}

		public void OnError (string error)
		{
			tokenTask.TrySetException (new Exception (error));
		}
		public string ClientId {get;set;}
		public List<string> Scope { get; set; }
		public Uri GetInitialUrl ()
		{
			var scope = string.Join("%20", Scope.Select (x => HttpUtility.UrlEncode (x)));
			var url = string.Format ("{0}client_id={1}&scope={2}&response_type=code&redirect_uri={3}", BaseUrl,ClientId,scope,RedirectUrl.AbsoluteUri);
			return new Uri(url);
		}

		public bool ClearCookiesBeforeLogin { get; set; }

		public bool HasCompleted { get; private set; }

	}
}

