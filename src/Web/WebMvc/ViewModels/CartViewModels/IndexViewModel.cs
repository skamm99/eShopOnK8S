﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebMvc.ViewModels.CartViewModels
{
    public class CartComponentViewModel
    {
        public int ItemsCount { get; set; }
        public string Disabled => (ItemsCount == 0) ? "is-disabled" : "";
    }
}
