using System;
using System.Collections.Generic;

namespace Execor.Models
{
    public class OverlayProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Default Profile";
        public List<WidgetConfig> Widgets { get; set; } = new List<WidgetConfig>();

        public OverlayProfile() { }
    }
}
