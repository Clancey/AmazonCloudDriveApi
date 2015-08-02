using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using MonoTouch.Dialog;
using Amazon.CloudDrive;
using System.IO;

namespace iOSSample
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		UIWindow window;

		//
		// This method is invoked when the application has loaded and is ready to run. In this
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);
			
			// If you have defined a root view controller, set it here:
			// window.RootViewController = myViewController;
			window.RootViewController = new UINavigationController(new RootViewController());
			// make the window visible
			window.MakeKeyAndVisible ();
			
			return true;
		}
	}

	public class RootViewController :  DialogViewController
	{
		public CloudDriveApi Api;
		public RootViewController() : base(null)
		{
			Api = new CloudDriveApi ("amazon-cloud-drive","ClientID", "ClientSecret");
			Root = new RootElement ("Cloud Drive") {
				new Section(){
					new StringElement("Sign in",async ()=>{
						var account = await Api.Authenticate();
						new UIAlertView("Logged in",account.ToString(),null,"Ok").Show();
					}),
					new StringElement("Get Account Info",async ()=>{
						var accountInfo = await Api.GetAccountInfo();
						new UIAlertView("User info",accountInfo.ToString(),null,"Ok").Show();
					}),
				},
				new Section("Files"){
					new StringElement("List files",async()=>{
						var files = await Api.GetNodeList(new CloudNodeRequest{
							Limit = 10,
						});
						var fileRoot = new RootElement("Files"){
							new Section(){
								files.Data.Select(x=> new StringElement(x.Name)),
							}
						};
						this.NavigationController.PushViewController(new DialogViewController(fileRoot,true),true);
					}),
					new StringElement("Upload File",async () => {
						var file = "TestUploadFile.txt";
						var upload  = await Api.UploadFile(new FileUploadData(file),file);
						Console.WriteLine(upload);
					}),
					new StringElement("Get Changes",GetChanges),
				},
			};
		}

		public async void GetChanges()
		{
			var result = await Api.GetChanges ();
			int totalCount = 0;
			while (result.HasMore) {
				var items = await result.LoadMoreNodes ();
				totalCount += items?.Count ?? 0;
				Console.WriteLine (items.Count);
			}
			Console.WriteLine ($"Finished collecting Changes - {totalCount}");
		}
	}
}

