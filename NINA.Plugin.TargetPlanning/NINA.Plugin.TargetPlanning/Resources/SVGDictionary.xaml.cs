
using System;
using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Plugin.TargetPlanning.Resources {

    [Export(typeof(ResourceDictionary))]
    public partial class SVGDictionary : ResourceDictionary {

        public SVGDictionary() {
            InitializeComponent();
        }
    }
}