using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Fiddler;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using SRTools.Views.GachaViews;
using Spectre.Console;
using Newtonsoft.Json.Linq;
using Windows.UI.Core;
using System.Net.Http;
using Vanara.PInvoke;

namespace SRTools.Views
{
    public sealed partial class DonationView : Page
    {
        public DonationView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to DonationView", 0);
            
            
        }
        
    }
}