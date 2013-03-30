﻿using MetroExplorer.core;
using MetroExplorer.core.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace MetroExplorer
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class PageExplorer : MetroExplorer.Common.LayoutAwarePage, INotifyPropertyChanged
    {
        DispatcherTimer _imageChangingDispatcher = new DispatcherTimer();

        private void InitializeChangingDispatcher()
        {
            _imageChangingDispatcher.Tick += ImageChangingDispatcher_Tick;
            _imageChangingDispatcher.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _imageChangingDispatcher.Start();
        }

        int _loadingImageCount = 0;
        int _loadingFolderImageCount = 0;
        bool _imageDispatcherLock = false;
        async void ImageChangingDispatcher_Tick(object sender, object e)
        {
            if (_imageDispatcherLock == false && ExplorerGroups != null)
            {
                _imageDispatcherLock = true;
                if (ExplorerGroups != null && ExplorerGroups[1] != null && _loadingImageCount < ExplorerGroups[1].Count)
                {
                    for (int i = 1; i % 20 != 0 && ExplorerGroups != null && _loadingImageCount < ExplorerGroups[1].Count; i++)
                    {
                        await ThumbnailPhoto(ExplorerGroups[1][_loadingImageCount], ExplorerGroups[1][_loadingImageCount].StorageFile);
                        _loadingImageCount++;
                    }
                }
                else
                {
                    _imageChangingDispatcher.Interval = new TimeSpan(0, 0, 0, 2);
                }
                await ChangeFolderCover();
                _imageDispatcherLock = false;
            }
            LoadingProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async Task ChangeFolderCover()
        {
            if (_loadingFolderImageCount % 7 == 0 && ExplorerGroups != null && ExplorerGroups[0] != null)
            {
                for (int i = 0; ExplorerGroups != null && ExplorerGroups[0] != null && i < ExplorerGroups[0].Count; i++)
                {
                    var sdf = (await ExplorerGroups[0][i].StorageFolder.GetFilesAsync()).Where(p => p.Name.ToUpper().EndsWith(".JPG") || p.Name.ToUpper().EndsWith(".JPEG")
                                    || p.Name.ToUpper().EndsWith(".PNG") || p.Name.ToUpper().EndsWith(".BMP")).ToList();
                    if (sdf != null && sdf.Count() > 0)
                    {
                        await ThumbnailPhoto(ExplorerGroups[0][i], sdf[(new Random()).Next(sdf.Count)]);
                    }
                }
            }
            _loadingFolderImageCount = ++_loadingFolderImageCount % 7;
        }

        private async void AddNewItem(GroupInfoList<ExplorerItem> itemList, IStorageItem retrievedItem)
        {
            ExplorerItem item = new ExplorerItem()
            {
                Name = retrievedItem.Name,
                Path = retrievedItem.Path
            };
            if (retrievedItem is StorageFolder)
            {
                item.StorageFolder = retrievedItem as StorageFolder;
                item.Type = ExplorerItemType.Folder;
            }
            else if (retrievedItem is StorageFile)
            {
                item.StorageFile = retrievedItem as StorageFile;
                item.Type = ExplorerItemType.File;
                item.Size = (await item.StorageFile.GetBasicPropertiesAsync()).Size;
                item.ModifiedDateTime = (await item.StorageFile.GetBasicPropertiesAsync()).DateModified.DateTime;
            }
            itemList.Add(item);
        }

        private async System.Threading.Tasks.Task ThumbnailPhoto(ExplorerItem item, StorageFile sf)
        {
            if (item == null) return;
            StorageItemThumbnail fileThumbnail = await sf.GetThumbnailAsync(ThumbnailMode.SingleItem, 250);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(fileThumbnail);
            item.Image = bitmapImage;
        }

        private void ExplorerItemImage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void Button_RemoveDiskFolder_Click(object sender, RoutedEventArgs e)
        {
            if (itemGridView.SelectedItems == null || itemGridView.SelectedItems.Count == 0) return;
            while (itemGridView.SelectedItems.Count > 0)
            {
                if (ExplorerGroups[0].Contains(itemGridView.SelectedItems[0] as ExplorerItem))
                {
                    await (itemGridView.SelectedItems[0] as ExplorerItem).StorageFolder.DeleteAsync();
                    ExplorerGroups[0].Remove(itemGridView.SelectedItems[0] as ExplorerItem);
                }
                else if (ExplorerGroups[1].Contains(itemGridView.SelectedItems[0] as ExplorerItem))
                {
                    await (itemGridView.SelectedItems[0] as ExplorerItem).StorageFile.DeleteAsync();
                    ExplorerGroups[1].Remove(itemGridView.SelectedItems[0] as ExplorerItem);
                }
            }
            BottomAppBar.IsOpen = false;
        }

        private void Button_RenameDiskFolder_Click(object sender, RoutedEventArgs e)
        {
            if (itemGridView.SelectedItems.Count == 1)
            {
                (itemGridView.SelectedItem as ExplorerItem).RenameBoxVisibility = "Visible";
            }
        }

        private void AppBar_BottomAppBar_Opened_1(object sender, object e)
        {
            if (itemGridView.SelectedItems.Count == 1)
            {
                Button_RenameDiskFolder.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                Button_RenameDiskFolder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            if (itemGridView.SelectedItems.Count == 0)
            {
                Button_RemoveDiskFolder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                Button_RemoveDiskFolder.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private async void ItemGridView_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            ExplorerItem item = e.ClickedItem as ExplorerItem;
            if (item.Type == ExplorerItemType.Folder)
            {
                //this.Frame.Navigate(typeof(PageExplorer), item.StorageFolder);
                _navigatorStorageFolders.Add(item.StorageFolder);
                Frame.Navigate(typeof(PageExplorer), _navigatorStorageFolders);
            }
            else if (item.Type == ExplorerItemType.File)
            {
                if (item.StorageFile != null && item.StorageFile.IsImageFile())
                {
                    this.Frame.Navigate(typeof(PhotoGallery), new Object[] { _navigatorStorageFolders, item.StorageFile });
                }
                else
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(item.Path);
                    var targetStream = await file.OpenAsync(FileAccessMode.Read);
                    await Launcher.LaunchFileAsync(file, new LauncherOptions { DisplayApplicationPicker = true });
                }
            }
        }

        private async void Button_AddNewFolder_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder sf = await _currentStorageFolder.CreateFolderAsync(StringResources.ResourceLoader.GetString("String_NewFolder"), CreationCollisionOption.GenerateUniqueName);
            ExplorerItem item = new ExplorerItem()
            {
                Name = sf.Name,
                RenamingName = sf.Name,
                Path = sf.Path,
                Type = ExplorerItemType.Folder,
                RenameBoxVisibility = "Visible",
                StorageFolder = sf
            };
            ExplorerGroups[0].Add(item);
            itemGridView.SelectedItem = item;
        }

        private void ItemGridView_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void ItemGridView_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            foreach (var selectedItem in e.RemovedItems)
            {
                if ((selectedItem as ExplorerItem).RenameBoxVisibility == "Visible")
                    (selectedItem as ExplorerItem).RenameBoxVisibility = "Collapsed";
            }
            foreach (var selectedItem in itemGridView.SelectedItems)
            {
                if (itemGridView.SelectedItems.Count > 1 && (selectedItem as ExplorerItem).RenameBoxVisibility == "Visible")
                    (selectedItem as ExplorerItem).RenameBoxVisibility = "Collapsed";
            }
            if (itemGridView.SelectedItems.Count == 1 && (itemGridView.SelectedItems[0] as ExplorerItem).RenameBoxVisibility == "Visible")
                BottomAppBar.IsOpen = false;
            else if (itemGridView.SelectedItems.Count > 0)
                BottomAppBar.IsOpen = true;
        }

        private void Button_CancelRename_Click(object sender, RoutedEventArgs e)
        {
            if (itemGridView.SelectedItem != null)
            {
                (itemGridView.SelectedItem as ExplorerItem).RenameBoxVisibility = "Collapsed";
            }
        }

        private async void Button_RenameFolder_Click(object sender, RoutedEventArgs e)
        {
            if (itemGridView.SelectedItem != null)
            {
                (itemGridView.SelectedItem as ExplorerItem).Name = (itemGridView.SelectedItem as ExplorerItem).RenamingName;
                (itemGridView.SelectedItem as ExplorerItem).RenameBoxVisibility = "Collapsed";
                if ((itemGridView.SelectedItem as ExplorerItem).Type == ExplorerItemType.Folder)
                    await (itemGridView.SelectedItem as ExplorerItem).StorageFolder.RenameAsync((itemGridView.SelectedItem as ExplorerItem).RenamingName, NameCollisionOption.GenerateUniqueName);
                else if ((itemGridView.SelectedItem as ExplorerItem).Type == ExplorerItemType.File)
                    await (itemGridView.SelectedItem as ExplorerItem).StorageFile.RenameAsync((itemGridView.SelectedItem as ExplorerItem).RenamingName, NameCollisionOption.GenerateUniqueName);
            }
        }

        private void ExplorerItemImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //(sender as Image).FadeOut();
            (sender as Image).FadeInCustom(new TimeSpan(0, 0, 0, 1, 500));
        }

        private Boolean IsImageFile(StorageFile file)
        {
            if (file.FileType.ToUpper().Equals(".JPG") ||
                file.FileType.ToUpper().Equals(".JPEG") ||
                file.FileType.ToUpper().Equals(".PNG") ||
                file.FileType.ToUpper().Equals(".BMP"))
                return true;
            return false;
        }
    }

    /// <summary>
    /// Bottom App bar right buttons
    /// </summary>
    public sealed partial class PageExplorer : MetroExplorer.Common.LayoutAwarePage, INotifyPropertyChanged
    {
        private async void Button_CutPaste_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder storageFolder = await GetAPickedFolder();
        }

        private async void Button_CopyPaste_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder storageFolder = await GetAPickedFolder();
        }

        private async Task<StorageFolder> GetAPickedFolder()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.ViewMode = PickerViewMode.Thumbnail;
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");
            return await folderPicker.PickSingleFolderAsync();
        }

        private void Button_Detail_Click(object sender, RoutedEventArgs e)
        {
            if (itemGridView.ItemTemplate == this.Resources["Standard300x80ItemTemplate"] as DataTemplate)
            {
                itemGridView.ItemTemplate = this.Resources["Standard300x180ItemTemplate"] as DataTemplate;
                PageExplorer.BigSquareMode = true;
            }
            else
            {
                itemGridView.ItemTemplate = this.Resources["Standard300x80ItemTemplate"] as DataTemplate;
                PageExplorer.BigSquareMode = false;
            }
        }
    }
}