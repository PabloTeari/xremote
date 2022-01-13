// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace xRemoteApp
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSPopUpButton DevicesFind { get; set; }

		[Action ("ConnectButton:")]
		partial void ConnectButton (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (DevicesFind != null) {
				DevicesFind.Dispose ();
				DevicesFind = null;
			}
		}
	}
}
