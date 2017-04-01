using System;
using System.Windows.Controls;

namespace ReorderWPF.CustomControls
{
    class PageFrameTab : TabItem
    {

        public PageFrameTab(ThreadedPage Page)
        {
            Header = Page.Title;
            Frame Frame = new Frame();
            Frame.Content = Page;
            base.Content = Frame;
            ContentRef = Page;
        }


        ThreadedPage ContentRef;
        internal Type GetChildType()
        {
            return ((Frame)base.Content).Content.GetType();
        }

        internal ThreadedPage Content
        {
            get { return ContentRef; }
        }


    }
}
