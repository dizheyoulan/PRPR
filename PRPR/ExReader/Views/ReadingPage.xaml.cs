﻿using Microsoft.QueryStringDotNET;
using PRPR.Common;
using PRPR.Common.Services;
using PRPR.ExReader.Models;
using PRPR.ExReader.Services;
using PRPR.ExReader.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PRPR.ExReader.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadingPage : Page
    {
        public ReadingPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }


        #region NavigationHelper

        private NavigationHelper navigationHelper;
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion



        public ReadingViewModel ReadingViewModel
        {
            get
            {
                return this.DataContext as ReadingViewModel;
            }
            set
            {
                this.DataContext = value;
            }
        }



        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // Load Images
            try
            {
                var galleryLinkFromLastPage = QueryString.Parse(e.NavigationParameter as string)["link"];

                // Get ExGallery
                if (App.Current.Resources.ContainsKey("Gallery"))
                {
                    this.ReadingViewModel.Gallery = App.Current.Resources["Gallery"] as ExGallery;
                }
                else
                {
                    this.ReadingViewModel = new ReadingViewModel();
                    this.ReadingViewModel.Gallery = await ExGallery.DownloadGalleryAsync(galleryLinkFromLastPage, 1, 3);
                }

                // Load all image item
                var images = new ObservableCollection<ExImage>();
                await this.ReadingViewModel.Gallery.LoadAllItemsAsync();
                foreach (var item in this.ReadingViewModel.Gallery)
                {
                    images.Add(new ExImage() { Link = item.Link, Thumb = item.Thumb });
                }
                this.ReadingViewModel.Images = images;

                // Jump to Page
                var indexFromLastPage = int.Parse(QueryString.Parse(e.NavigationParameter as string)["page"]);
                this.ReadingViewModel.CurrentImageIndex = indexFromLastPage;


            }
            catch (Exception ex)
            {

            }

        }


        private void HandleConnectedAnimation()
        {
            try
            {
                var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("ThumbImage");

                if (animation != null)
                {

                    // Connect the animation to only the scrolling host of the flipview
                    // So the prev/next buttons will not be animationed
                    var grid = VisualTreeHelper.GetChild(flipView, 0) as Grid;
                    var scrollingHost = grid.Children.FirstOrDefault(o => o is ScrollViewer) as UIElement;
                    animation.TryStart(scrollingHost as UIElement);
                }
            }
            catch (Exception ex)
            {

            }
        } 


        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (this.ReadingViewModel.Gallery != null)
            {
                e.PageState["Gid"] = this.ReadingViewModel.Gallery.Gid;
                e.PageState["Link"] = this.ReadingViewModel.Gallery.Link;
                e.PageState["Page"] = this.ReadingViewModel.CurrentImageIndex;
            }

            try
            {
                var container = flipView.ContainerFromIndex(flipView.SelectedIndex) as FlipViewItem;
                var imageView = container.ContentTemplateRoot as ImageView;
                var srollViewer = ((imageView.Content as Grid).Children.First() as ScrollViewer);
                var iamgeGrid = srollViewer.Content as Grid;
               
                // Prepare backward connected animation
                var grid = VisualTreeHelper.GetChild(flipView, 0) as Grid;
                var scrollingHost = grid.Children.FirstOrDefault(o => o is ScrollViewer) as UIElement;
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ThumbImage", iamgeGrid);
            }
            catch (Exception ex)
            {
                
            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollViewer = (sender as ScrollViewer);
            (scrollViewer.Content as FrameworkElement).MaxHeight = e.NewSize.Height;
            (scrollViewer.Content as FrameworkElement).MaxWidth = e.NewSize.Width;
        }

        private void BitmapImage_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            Debug.WriteLine(e.Progress);
           
        }

        private void ScrollViewer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var scrollViewer = (sender as ScrollViewer);


            if (scrollViewer.ZoomFactor == 1)
            {
                scrollViewer.ChangeView(e.GetPosition(scrollViewer).X, e.GetPosition(scrollViewer).Y, 2, true);
            }
            else
            {
                scrollViewer.ChangeView(null, null, 1, true);
            }
        }

        bool ButtonsOverlaying = false ;
        private void ScrollViewer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Toggle the buttons display
            if (!ButtonsOverlaying)
            {
                ButtonsOverlaying = true;
                var b = VisualStateManager.GoToState(CurrentReadingPage, "ButtonsOnly", true);
            }
            else
            {
                ButtonsOverlaying = false;
                var b = VisualStateManager.GoToState(CurrentReadingPage, "NoOverlay", true);
            }


            e.Handled = true;
        }

        private void ScrollViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // Reset the zoom if the image is changed
            var scrollViewer = (sender as ScrollViewer);
            // Reset the scroll and zoom of the image
            scrollViewer.ZoomToFactor(1);
        }




        private void Image_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

            var x = FlyoutBase.GetAttachedFlyout((FrameworkElement)sender);

            dummyGrid.Margin = new Thickness(e.GetPosition(flipView).X, e.GetPosition(flipView).Y, 0, 0);
            x.ShowAt(dummyGrid);

            e.Handled = true;
        }

        private void Image_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                var x = FlyoutBase.GetAttachedFlyout((FrameworkElement)sender);

                dummyGrid.Margin = new Thickness(e.GetPosition(flipView).X, e.GetPosition(flipView).Y, 0, 0);
                x.ShowAt(dummyGrid);

                e.Handled = true;
            }
        }




        private async void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            IBuffer imageBuffer = null;

            if (this.ReadingViewModel.Images.Count != 0 && this.ReadingViewModel.CurrentImageIndex != -1)
            {
                var source = this.ReadingViewModel.Images[this.ReadingViewModel.CurrentImageIndex].ImageSource;

                var hc = new HttpClient();
                imageBuffer = await hc.GetBufferAsync(new Uri(source));
            }

            await ClipboardService.CopyImageAsync(imageBuffer);
        }

        private void CurrentReadingPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Handle the connected animation
            HandleConnectedAnimation();
        }
    }
}
