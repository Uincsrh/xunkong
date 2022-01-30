﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Xunkong.Desktop.Controls
{

    public sealed partial class WebToolContentControl : UserControl
    {

        private readonly WebToolItem _webToolItem;

        internal WebToolItem WebToolItem => _webToolItem;

        internal WebToolContentControl(WebToolItem item)
        {
            this.InitializeComponent();
            Loaded += WebToolContentControl_LoadedAsync;
            _webToolItem = item;
            _WebView2.Source = new Uri(item.Url);
            _WebView2.NavigationStarting += _WebView2_NavigationStarting;
        }

        private void _WebView2_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            _TextBox_Address.Text = args.Uri.ToString();
        }

        private async void WebToolContentControl_LoadedAsync(object sender, RoutedEventArgs e)
        {
            await _WebView2.EnsureCoreWebView2Async();
            _WebView2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            _WebView2.CoreWebView2.NewWindowRequested += (sender, args) => args.Handled = true;
        }


        private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess && !string.IsNullOrWhiteSpace(_webToolItem?.JavaScript))
            {
                await _WebView2.CoreWebView2.ExecuteScriptAsync(_webToolItem.JavaScript);
            }
        }


        [ICommand]
        private void GoBack()
        {
            if (_WebView2.CanGoBack)
            {
                _WebView2.GoBack();
            }
        }


        [ICommand]
        private void GoForward()
        {
            if (_WebView2.CanGoForward)
            {
                _WebView2.GoForward();
            }
        }


        [ICommand]
        private void Refresh()
        {
            _WebView2.Reload();
        }


        private void _TextBox_Address_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var address = _TextBox_Address.Text;
                _WebView2.Source = new Uri(address);
            }
        }
    }
}