﻿using CommunityToolkit.WinUI.Notifications;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Services.Store;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using Xunkong.ApiClient;
using Xunkong.Desktop.Controls;
using Xunkong.Hoyolab;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Xunkong.Desktop.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class HomePage : Page
{


    private const string FallbackWallpaperUri = "ms-appx:///Assets/Images/96839227_p0.webp";
    private static WallpaperInfo FallbackWallpaper = new WallpaperInfo
    {
        Title = "Rest",
        Author = "Sigrixxx",
        Description = "她们太般配了啊！！！",
        FileName = "[Sigrixxx] Rest [96839227_p0].webp",
        Source = "https://www.pixiv.net/artworks/96839227",
        Url = "https://file.xunkong.cc/wallpaper/96839227_p0.webp"
    };

    private readonly XunkongApiService _xunkongApiService;

    private readonly HoyolabClient _hoyolabClient;


    public HomePage()
    {
        this.InitializeComponent();
        _xunkongApiService = ServiceProvider.GetService<XunkongApiService>()!;
        _hoyolabClient = ServiceProvider.GetService<HoyolabClient>()!;
        Loaded += HomePage_Loaded;
    }





    [ObservableProperty]
    private WallpaperInfo wallpaperInfo;


    [ObservableProperty]
    private string monthName;

    [ObservableProperty]
    private List<HomePage_DayData>? materialWeekData;


    [ObservableProperty]
    private List<HomePage_MaterialData>? materialDayData;


    [ObservableProperty]
    private List<Hoyolab.Wiki.Activity>? activityData;

    [ObservableProperty]
    private List<Hoyolab.Wiki.Activity>? strategyData;


   


    private async void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeWallpaper();
        await InitializeCalendarAsync();
        await InitializeActivityAsync();
        _Pivot_MaterialAndActivity.Visibility = Visibility.Visible;
        await InitializeInfoBarContentAsync();
        await CheckUpdateAndShowInfoBarAsync();
    }



    #region Wallpaper


    /// <summary>
    /// 图片最大高度
    /// </summary>
    private double imageMaxHeight;
    /// <summary>
    /// 图片高宽比
    /// </summary>
    private double heightDivWidth;
    private Compositor _compositor;
    private SpriteVisual imageVisual;
    private LoadedImageSurface imageSurface;
    private CompositionSurfaceBrush imageBrush;
    private CompositionLinearGradientBrush linearGradientBrush1;
    private CompositionLinearGradientBrush linearGradientBrush2;
    private CompositionEffectBrush gradientEffectBrush;
    private CompositionEffectBrush opacityEffectBrush;
    private CompositionEffectBrush maskEffectBrush;


    /// <summary>
    /// 初始化推荐图片，并且下载新的
    /// </summary>
    private async void InitializeWallpaper()
    {
        try
        {
            if (!AppSetting.GetValue(SettingKeys.EnableHomePageWallpaper, true))
            {
                return;
            }
            _Grid_Image.Visibility = Visibility.Visible;
            var wallpaper = _xunkongApiService.GetPreparedWallpaper();
            string file;
            if (wallpaper is null)
            {
                WallpaperInfo = FallbackWallpaper;
                file = (await StorageFile.GetFileFromApplicationUriAsync(new(FallbackWallpaperUri))).Path;
            }
            else
            {
                var cachedFile = CacheHelper.GetCacheFilePath(wallpaper.Url);
                if (File.Exists(cachedFile))
                {
                    WallpaperInfo = wallpaper;
                    file = cachedFile;
                }
                else
                {
                    WallpaperInfo = FallbackWallpaper;
                    file = (await StorageFile.GetFileFromApplicationUriAsync(new(FallbackWallpaperUri))).Path;
                }
            }
            imageMaxHeight = MainWindow.Current.Height * 0.75 / MainWindow.Current.UIScale;
            _Grid_Image.MaxHeight = imageMaxHeight;
            await LoadBackgroundImage(file);
            c_StackPanel_QuickAction.Visibility = Visibility.Visible;
            if (NetworkHelper.IsInternetOnMeteredConnection)
            {
                if (!AppSetting.GetValue<bool>(SettingKeys.DownloadWallpaperOnMeteredInternet))
                {
                    // 使用按流量计费的网络时，不下载新图片
                    return;
                }
            }
            var maxage = AppSetting.GetValue<int>(SettingKeys.WallpaperRefreshTime) switch
            {
                0 => 5,
                1 => 3600 * 1,
                2 => 3600 * 4,
                3 => 3600 * 12,
                _ => 5,
            };
            await _xunkongApiService.PrepareNextWallpaperAsync(maxage);
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "加载推荐图片");
            Logger.Error(ex, "加载推荐图片");
        }
        finally
        {
            c_StackPanel_QuickAction.Visibility = Visibility.Visible;
        }
    }


    /// <summary>
    /// 加载背景图片
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private async Task LoadBackgroundImage(string file)
    {
        try
        {
            _compositor = ElementCompositionPreview.GetElementVisual(_Border_BackgroundImage).Compositor;
            using var stream = File.OpenRead(file);
            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            heightDivWidth = (double)decoder.PixelHeight / decoder.PixelWidth;

            imageSurface = LoadedImageSurface.StartLoadFromUri(new(file));
            imageBrush = _compositor.CreateSurfaceBrush();
            imageBrush.Surface = imageSurface;
            imageBrush.Stretch = CompositionStretch.UniformToFill;
            imageBrush.VerticalAlignmentRatio = 0;

            linearGradientBrush1 = _compositor.CreateLinearGradientBrush();
            linearGradientBrush1.StartPoint = Vector2.Zero;
            linearGradientBrush1.EndPoint = Vector2.UnitY;
            linearGradientBrush1.ColorStops.Add(_compositor.CreateColorGradientStop(0, Colors.White));
            linearGradientBrush1.ColorStops.Add(_compositor.CreateColorGradientStop(0.95f, Colors.Black));

            linearGradientBrush2 = _compositor.CreateLinearGradientBrush();
            linearGradientBrush2.StartPoint = new Vector2(0.5f, 0);
            linearGradientBrush2.EndPoint = Vector2.UnitY;
            linearGradientBrush2.ColorStops.Add(_compositor.CreateColorGradientStop(0, Colors.White));
            linearGradientBrush2.ColorStops.Add(_compositor.CreateColorGradientStop(1, Colors.Black));

            var blendEffect = new BlendEffect
            {
                Mode = BlendEffectMode.Multiply,
                Background = new CompositionEffectSourceParameter("Background"),
                Foreground = new CompositionEffectSourceParameter("Foreground"),
            };

            var blendEffectFactory = _compositor.CreateEffectFactory(blendEffect);
            gradientEffectBrush = blendEffectFactory.CreateBrush();
            gradientEffectBrush.SetSourceParameter("Background", linearGradientBrush2);
            gradientEffectBrush.SetSourceParameter("Foreground", linearGradientBrush1);


            var invertEffect = new LuminanceToAlphaEffect
            {
                Source = new CompositionEffectSourceParameter("Source"),
            };

            var invertEffectFactory = _compositor.CreateEffectFactory(invertEffect);
            opacityEffectBrush = invertEffectFactory.CreateBrush();
            opacityEffectBrush.SetSourceParameter("Source", gradientEffectBrush);

            var maskEffect = new AlphaMaskEffect
            {
                AlphaMask = new CompositionEffectSourceParameter("Mask"),
                Source = new CompositionEffectSourceParameter("Source"),
            };

            var maskFactory = _compositor.CreateEffectFactory(maskEffect);
            maskEffectBrush = maskFactory.CreateBrush();
            maskEffectBrush.SetSourceParameter("Mask", opacityEffectBrush);
            maskEffectBrush.SetSourceParameter("Source", imageBrush);

            imageVisual = _compositor.CreateSpriteVisual();
            imageVisual.Brush = maskEffectBrush;

            var width = _Border_BackgroundImage.ActualWidth;
            var height = Math.Clamp(width * heightDivWidth, 0, imageMaxHeight);
            imageVisual.Size = new Vector2((float)width, (float)height);
            _Border_BackgroundImage.Height = height;
            _Border_BackgroundImage.Visibility = Visibility.Visible;
            ElementCompositionPreview.SetElementChildVisual(_Border_BackgroundImage, imageVisual);
        }
        catch (COMException ex)
        {
            // 部分设备会出现 COMExeption 组件初始化失败。（0x88982F8B）
            // 还有可能会出现其他不知道的错误
            // 原因不明，使用无透明度效果的图片代替
            _Image_BackgroundImage.Source = file;
            _StackPanel_BackgroundImage.Visibility = Visibility.Visible;
            Logger.Error(ex, "使用 Win2D 加载背景图片");
        }
    }


    /// <summary>
    /// 改变背景图片大小
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _Border_BackgroundImage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize == e.PreviousSize)
        {
            return;
        }
        var width = _Border_BackgroundImage.ActualWidth;
        var height = Math.Clamp(width * heightDivWidth, 0, imageMaxHeight);
        if (imageVisual is not null)
        {
            imageVisual.Size = new Vector2((float)width, (float)height);
            _Border_BackgroundImage.Height = height;
        }
    }


    /// <summary>
    /// 浏览图片大图
    /// </summary>
    [RelayCommand]
    private void OpenImageViewer()
    {
        if (WallpaperInfo is not null)
        {
            if (WallpaperInfo == FallbackWallpaper)
            {
                MainWindow.Current.SetFullWindowContent(new ImageViewer { Source = FallbackWallpaperUri });
            }
            else
            {
                MainWindow.Current.SetFullWindowContent(new ImageViewer { Source = CacheHelper.GetCacheFilePath(WallpaperInfo.Url) });
            }
        }
    }


    /// <summary>
    /// 复制图片
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CopyImageAsync()
    {
        try
        {
            StorageFile file;
            if (WallpaperInfo == FallbackWallpaper)
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(FallbackWallpaperUri));
            }
            else
            {
                var path = CacheHelper.GetCacheFilePath(WallpaperInfo.Url);
                if (!File.Exists(path))
                {
                    NotificationProvider.Warning("找不到缓存的文件", 3000);
                    return;
                }
                file = await StorageFile.GetFileFromPathAsync(path);
            }
            ClipboardHelper.SetBitmap(file);
            _Button_Copy.Content = "\xE8FB";
            await Task.Delay(3000);
            _Button_Copy.Content = "\xE8C8";
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "复制图片");
            Logger.Error(ex, "复制图片");
        }
    }



    /// <summary>
    /// 保存图片
    /// </summary>
    [RelayCommand]
    private void SaveWallpaper()
    {
        if (string.IsNullOrWhiteSpace(WallpaperInfo?.Url))
        {
            return;
        }
        try
        {
            string? file = null;
            if (WallpaperInfo == FallbackWallpaper)
            {
                file = StorageFile.GetFileFromApplicationUriAsync(new Uri(FallbackWallpaperUri)).GetAwaiter().GetResult().Path;
            }
            else
            {
                file = CacheHelper.GetCacheFilePath(WallpaperInfo.Url);
            }
            if (!File.Exists(file))
            {
                NotificationProvider.Warning("找不到缓存的文件", 3000);
                return;
            }
            var destFolder = Path.Combine(XunkongEnvironment.UserDataPath, "Wallpaper");
            var fileName = WallpaperInfo.FileName ?? Path.GetFileName(WallpaperInfo.Url);
            var destPath = Path.Combine(destFolder, fileName);
            Directory.CreateDirectory(destFolder);
            File.Copy(file, destPath, true);
            Action action = () => Process.Start(new ProcessStartInfo { FileName = destPath, UseShellExecute = true });
            NotificationProvider.ShowWithButton(InfoBarSeverity.Success, "已保存", fileName, "打开文件", action, null, 3000);
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "保存图片");
            Logger.Error(ex, "保存图片");
        }
    }


    /// <summary>
    /// 打开图源
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenImageSourceAsync()
    {
        if (string.IsNullOrWhiteSpace(WallpaperInfo?.Source))
        {
            return;
        }
        try
        {
            await Launcher.LaunchUriAsync(new Uri(WallpaperInfo.Source));
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "打开图源");
            Logger.Error(ex, "打开图源");
        }
    }


    #endregion




    #region 通知和检查更新

    /// <summary>
    /// 初始化通知栏内容
    /// </summary>
    /// <returns></returns>
    private async Task InitializeInfoBarContentAsync()
    {
        try
        {
            var list = await _xunkongApiService.GetInfoBarContentListAsync();
            if (list?.Any() ?? false)
            {
                _Grid_InfoBar.Visibility = Visibility.Visible;
                foreach (var item in list)
                {
                    InfoBar infoBar;
                    if (!string.IsNullOrWhiteSpace(item.ButtonContent) && !string.IsNullOrWhiteSpace(item.ButtonUri))
                    {
                        infoBar = NotificationProvider.Create((InfoBarSeverity)item.Severity, item.Title, item.Message, item.ButtonContent, async () => await Launcher.LaunchUriAsync(new Uri(item.ButtonUri)));
                    }
                    else
                    {
                        infoBar = NotificationProvider.Create((InfoBarSeverity)item.Severity, item.Title, item.Message);
                    }
                    _StackPanel_InfoBar.Children.Add(infoBar);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "主页通知栏内容");
        }
    }


    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns></returns>
    private async Task CheckUpdateAndShowInfoBarAsync()
    {
        try
        {
            if (XunkongEnvironment.IsStoreVersion)
            {
                // 商店版检查更新
                var context = StoreContext.GetDefault();
                // 调用需要在UI线程运行的函数前
                WinRT.Interop.InitializeWithWindow.Initialize(context, MainWindow.Current.HWND);
                var updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();
                if (updates.Any())
                {
                    _Grid_InfoBar.Visibility = Visibility.Visible;
                    Action action;
                    if (context.CanSilentlyDownloadStorePackageUpdates)
                    {
                        action = () =>
                        {
                            var operation = context.TrySilentDownloadAndInstallStorePackageUpdatesAsync(updates);
                            DownloadProgressHandle(operation);
                        };
                    }
                    else
                    {
                        action = () =>
                        {
                            var operation = context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);
                            DownloadProgressHandle(operation);
                        };
                    }
                    if (updates[0].Mandatory)
                    {
                        var stack1 = new StackPanel { Spacing = 8 };
                        stack1.Children.Add(new TextBlock { Text = "这是一个强制更新版本" });
                        var stack2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                        stack1.Children.Add(stack2);
                        var button1 = new Button { Content = "下载并安装" };
                        button1.Click += (_, _) =>
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "下载并安装更新");
                            }
                        };
                        stack2.Children.Add(button1);
                        var button2 = new Button { Content = "打开商店" };
                        button2.Click += async (_, _) => await Launcher.LaunchUriAsync(new(XunkongEnvironment.StoreProtocolUrl));
                        stack2.Children.Add(button2);
                        var dialog = new ContentDialog
                        {
                            Title = "有新版本",
                            Content = stack1,
                            XamlRoot = MainWindow.Current.XamlRoot,
                        };
                        await dialog.ShowWithZeroMarginAsync();
                    }
                    else
                    {
                        var infoBar = NotificationProvider.Create(InfoBarSeverity.Success, "有新版本", "商店版无法获取新版本的信息", "下载并安装", action);
                        _StackPanel_InfoBar.Children.Insert(0, infoBar);
                    }
                }
            }
            else
            {
                // 侧载版检查更新
                var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Xunkong"));
                var release = await client.Repository.Release.GetLatest("xunkong", "xunkong");
                if (Version.TryParse(release.TagName, out var version))
                {
                    if (version > XunkongEnvironment.AppVersion)
                    {
                        _Grid_InfoBar.Visibility = Visibility.Visible;
                        var infoBar = NotificationProvider.Create(InfoBarSeverity.Success, $"新版本 {version}", release.Name, "详细信息", async () => await Launcher.LaunchUriAsync(new Uri(release.HtmlUrl)));
                        _StackPanel_InfoBar.Children.Insert(0, infoBar);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "主页检查更新");
        }
    }


    /// <summary>
    /// 下载并安装新版本的进度条
    /// </summary>
    /// <param name="operation"></param>
    private static void DownloadProgressHandle(IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> operation)
    {
        const string tag = "download new version";
        const string group = "download";
        uint index = 0;
        var content = new ToastContentBuilder().AddText("别点我！").AddVisualChild(new AdaptiveProgressBar()
        {
            Title = "下载中",
            Value = new BindableProgressBarValue("progressValue"),
            ValueStringOverride = new BindableString("progressValueString"),
            Status = new BindableString("progressStatus")
        }).AddToastActivationInfo("DownloadNewVersion", ToastActivationType.Background).GetToastContent();

        var toast = new ToastNotification(content.GetXml());
        toast.Tag = tag;
        toast.Group = group;
        toast.Data = new NotificationData();
        toast.Data.Values["progressValue"] = "0";
        toast.Data.Values["progressValueString"] = "0%";
        toast.Data.Values["progressStatus"] = "0MB / 0MB";
        toast.Data.SequenceNumber = ++index;

        var manager = ToastNotificationManager.CreateToastNotifier();
        operation.Progress = (_, status) =>
        {
            if (status.PackageUpdateState == StorePackageUpdateState.Pending)
            {
                manager.Show(toast);
            }
            if (status.PackageUpdateState == StorePackageUpdateState.Downloading)
            {
                var progress = status.PackageDownloadProgress;
                var data = new NotificationData { SequenceNumber = ++index };
                data.Values["progressValue"] = $"{status.PackageDownloadProgress / 0.95}";
                data.Values["progressValueString"] = $"{status.PackageDownloadProgress / 0.95:P0}";
                data.Values["progressStatus"] = $"{status.PackageBytesDownloaded / (double)(1 << 20):F1}MB / {status.PackageDownloadSizeInBytes / (double)(1 << 20):F1}MB";
                manager.Update(data, tag, group);
            }
            if (status.PackageUpdateState == StorePackageUpdateState.Deploying)
            {
                manager.Hide(toast);
                Vanara.PInvoke.Kernel32.RegisterApplicationRestart(null, 0);
            }
        };
    }

    #endregion




    #region 素材和活动



    /// <summary>
    /// 初始化天赋材料日历
    /// </summary>
    /// <returns></returns>
    private async Task InitializeCalendarAsync()
    {
        try
        {
            var list = await _hoyolabClient.GetTalentCalenarsListAsync();
            var data = new List<HomePage_DayData>(7);
            var now = DateTimeOffset.Now;
            var monday = now.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            var characters = list.Where(x => x.BreakType == "2").ToList();
            var weapons = list.Where(x => x.BreakType == "1").ToList();
            for (int i = 0; i < 7; i++)
            {
                var day = monday.AddDays(i);
                var dayData = new HomePage_DayData
                {
                    Month = day.Month,
                    DayOfMonth = day.Day,
                    DayOfWeekName = DayOfWeekToString(day.DayOfWeek),
                    Data = new List<HomePage_MaterialData>()
                };
                var cs = characters.Where(x => x.DropDay.Contains($"{i + 1}")).GroupBy(x => x.ContentInfos.FirstOrDefault(x => x.Title.Contains("哲学"))).OrderBy(x => x.Key?.ContentId);
                foreach (var item in cs)
                {
                    var materialData = new HomePage_MaterialData { Name = item.Key?.Title!, Icon = item.Key?.Icon!, CharacterOrWeapon = item.OrderBy(x => x.Sort.GetValueOrDefault((int)day.DayOfWeek)).ToList() };
                    dayData.Data.Add(materialData);
                }
                var ws = weapons.Where(x => x.DropDay.Contains($"{i + 1}")).GroupBy(x => x.ContentInfos.MaxBy(x => x.ContentId)).OrderBy(x => x.Key?.ContentId);
                foreach (var item in ws)
                {
                    var materialData = new HomePage_MaterialData { Name = item.Key?.Title!, Icon = item.Key?.Icon!, CharacterOrWeapon = item.OrderBy(x => x.Sort.GetValueOrDefault((int)day.DayOfWeek)).ToList() };
                    dayData.Data.Add(materialData);
                }
                data.Add(dayData);
            }
            MaterialWeekData = data;
            var index = ((int)now.DayOfWeek + 6) % 7;
            _GridView_DaySelction.SelectedIndex = index;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "天赋材料日历");
        }
    }


    private string DayOfWeekToString(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "一",
            DayOfWeek.Tuesday => "二",
            DayOfWeek.Wednesday => "三",
            DayOfWeek.Thursday => "四",
            DayOfWeek.Friday => "五",
            DayOfWeek.Saturday => "六",
            DayOfWeek.Sunday => "日",
            _ => ""
        };
    }


    /// <summary>
    /// 天赋材料日历按日切换
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _GridView_DaySelction_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var day = MaterialWeekData?.Skip(_GridView_DaySelction.SelectedIndex).FirstOrDefault();
        if (day != null)
        {
            MonthName = $"{day.Month}月";
            MaterialDayData = day.Data;
        }
    }


    /// <summary>
    /// 初始化活动内容
    /// </summary>
    /// <returns></returns>
    private async Task InitializeActivityAsync()
    {
        try
        {
            (ActivityData, StrategyData) = await _hoyolabClient.GetGameActivitiesListAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "活动内容");
        }
    }


    /// <summary>
    /// 天赋材料列表左移
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _Button_MaterialLeft_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement button)
        {
            if (button.Tag is ScrollViewer scroll)
            {
                scroll.ChangeView(scroll.HorizontalOffset - 216, null, null);
            }
        }
    }


    /// <summary>
    /// 天赋材料列表右移
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _Button_MaterialRight_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement button)
        {
            if (button.Tag is ScrollViewer scroll)
            {
                scroll.ChangeView(scroll.HorizontalOffset + 216, null, null);
            }
        }
    }


    /// <summary>
    /// 打开活动链接
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as HyperlinkButton)?.DataContext is Hoyolab.Wiki.Activity activity)
        {
            await Launcher.LaunchUriAsync(new Uri(activity.Url));
        }
    }


    #endregion




    #region 快捷操作


    [RelayCommand]
    private async Task StartGameAsync()
    {
        if (await InvokeService.StartGameAsync(true))
        {
            await InvokeService.CheckTransformerReachedAndHomeCoinFullAsync(true);
        }
    }




    #endregion




}
