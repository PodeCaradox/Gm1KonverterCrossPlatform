﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Gm1KonverterCrossPlatform.HelperClasses.Views
{
    public class MessageBoxWindow : Window
    {
        public enum MessageTyp { Fehler,Info }
        public MessageBoxWindow(MessageTyp typ,string message)
        {
          
            
            this.InitializeComponent();
            #if DEBUG
            this.AttachDevTools();
            #endif
            this.Title = typ.ToString();
            var textBox = this.Get<TextBlock>("textBox");
            textBox.Text = message;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

        }

            private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}