﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PRPR.Common.Controls
{
    public partial class JustifiedWrapPanel
    {
        bool IsItemItsOwnContainer(object item)
        {
            return item is ContentControl && (item as ContentControl).Content == item;
        }

        DependencyObject GetContainerForItem()
        {
            return new ContentControl();
        }

        void ClearContainerForItem(DependencyObject element, object item)
        {
            if (!IsItemItsOwnContainer(item))
            {
                if (element is ContentControl container)
                {
                    container.Content = null;
                    container.ContentTemplate = null;
                    container.Style = null;
                }
            }
        }

        void PrepareContainerForItem(DependencyObject element, object item)
        {
            if (!IsItemItsOwnContainer(item))
            {
                if (element is ContentControl container)
                {
                    container.Content = item;
                    container.ContentTemplate = ItemTemplate;
                    container.Style = ItemContainerStyle;
                }
            }
        }

        void OnItemsChanged(object e)
        {

        }

        void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)
        {

        }

        void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)
        {

        }

        void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {

        }


        void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)
        {

        }

        void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)
        {

        }

        object ItemFromContainer(DependencyObject container)
        {
            return (container as ContentControl).Content;
        }

        DependencyObject ContainerFromItem(object item)
        {
            return Containers.FirstOrDefault(o => o.Content == item);
        }


        //int IndexFromContainer(DependencyObject container)
        //{

        //}


        //DependencyObject ContainerFromIndex(int index)
        //{

        //}

        List<ContentControl> RecycledContainers = new List<ContentControl>();

        List<ContentControl> Containers = new List<ContentControl>();

        void RevirtualizeAll()
        {


            if (ItemsSource is IList items)
            {
                foreach (var item in Containers)
                {
                    if (true)
                    {

                    }
                }

                var itemsToRealize = new List<object>();
                for (int i = FirstActive; i <= LastActive; i++)
                {
                    itemsToRealize.Add(items[i]);
                }

                var reusableContainers = new List<ContentControl>();
                foreach (var container in Containers)
                {
                    if (!itemsToRealize.Contains(container.Content))
                    {
                        itemsToRealize.Remove(container.Content);
                    }
                    else
                    {
                        reusableContainers.Add(container);
                    }
                }

                // Reuse some container without full recycle and realize
                // As many as possible
                while (reusableContainers.Count != 0 && itemsToRealize.Count != 0)
                {
                    var container = reusableContainers.First();
                    var item = itemsToRealize.First();
                    container.Content = item;

                    reusableContainers.Remove(container);
                    itemsToRealize.Remove(item);
                }


                // Recycle / Realize the rest
                foreach (var container in reusableContainers)
                {
                    RecycleItem(container.Content);
                }
                foreach (var item in itemsToRealize)
                {
                    RealizeItem(item);
                }
            }

        }

        void RecycleItem(object item)
        {
            var container = ContainerFromItem(item);
            if (container != null)
            {
                ClearContainerForItem(container, item);
                Containers.Remove(container as ContentControl);
                Children.Remove(container as ContentControl);
                RecycledContainers.Add(container as ContentControl);
            }
        }

        void RealizeItem(object item)
        {
            // Check if already realized
            if (ContainerFromItem(item) != null)
            {
                return;
            }


            ContentControl container;
            if (IsItemItsOwnContainer(item))
            {
                container = item as ContentControl;
            }
            else
            {
                // Reuse container if possible
                if (RecycledContainers.Count > 0)
                {
                    container = RecycledContainers.First();
                    RecycledContainers.Remove(container);
                }
                else
                {
                    container = (ContentControl)GetContainerForItem();
                }
            }

            if (container == null)
            {
                // TODO: debug
                return;
            }


            PrepareContainerForItem(container, item);
            Containers.Add(container);
            
            Children.Add(container);
        }

        void RecycleAll()
        {
            this.Children.Clear();
            while (Containers.Count != 0)
            {
                var container = Containers.First();
                if (container != null)
                {
                    ClearContainerForItem(container, container.Content);
                    Containers.Remove(container as ContentControl);
                    RecycledContainers.Add(container as ContentControl);
                }
            }
        }
    }
}