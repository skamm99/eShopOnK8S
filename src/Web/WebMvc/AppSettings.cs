﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebMvc
{
    public class AppSettings
    {
        public string ConnectionString { get; set; }
        public string MarketingUrl { get; set; }
        public string PurchaseUrl { get; set; }
        public string SignalrHubUrl { get; set; }
        public string CatalogUrl { get; set; }
        public string OrderingUrl { get; set; }
        public string BasketUrl { get; set; }
        public string IdentityUrl { get; set; }
        public string CallBackUrl { get; set; }
        public string LocationsUrl { get; set; }
        public bool ActivateCampaignDetailFunction { get; set; }
        public bool UseResilientHttp { get; set; }
        public Logging Logging { get; set; }
        public bool UseCustomizationData { get; set; }
    }

    public class Connectionstrings
    {
        public string DefaultConnection { get; set; }
    }

    public class Logging
    {
        public bool IncludeScopes { get; set; }
        public Loglevel LogLevel { get; set; }
    }

    public class Loglevel
    {
        public string Default { get; set; }
        public string System { get; set; }
        public string Microsoft { get; set; }
    }
}
