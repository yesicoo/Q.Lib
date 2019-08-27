﻿using Q.Lib.Extension;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Model
{
   public class AckItem
    {
        public int ResCode { set; get; }
        public string ResDesc { set; get; } = "OK";
        public dynamic ResData { set; get; } = new ExpandoObject();

        public AckItem()
        {

        }

        public AckItem(int code, string resDesc)
        {
            this.ResCode = code;
            this.ResDesc = resDesc;
        }

        public AckItem(dynamic resData)
        {
            this.ResData = resData;
        }

        public T GetData<T>()
        {
            Json.Convert2T<T>(this.ResData);
        }
    }
}
