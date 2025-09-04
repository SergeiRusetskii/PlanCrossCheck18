﻿using System;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        public void Execute(ScriptContext context, Window window)
        {
            if (context.Course == null)
            {
                MessageBox.Show("Please load a plan before running this script.", "No Course Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mainControl = new PlanCrossCheck.MainControl
            {
                DataContext = new PlanCrossCheck.ValidationViewModel(context)
            };

            window.Content = mainControl;
            window.Title = "Cross-check v1.5.7";
            window.Width = 650;
            window.Height = 1000;
        }
    }
}