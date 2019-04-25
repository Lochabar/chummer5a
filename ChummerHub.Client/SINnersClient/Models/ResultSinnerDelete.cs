﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace SINners.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;

    public partial class ResultSinnerDelete
    {
        /// <summary>
        /// Initializes a new instance of the ResultSinnerDelete class.
        /// </summary>
        public ResultSinnerDelete() { }

        /// <summary>
        /// Initializes a new instance of the ResultSinnerDelete class.
        /// </summary>
        public ResultSinnerDelete(object myException = default(object), bool? callSuccess = default(bool?), string errorText = default(string))
        {
            MyException = myException;
            CallSuccess = callSuccess;
            ErrorText = errorText;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "myException")]
        public object MyException { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "callSuccess")]
        public bool? CallSuccess { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "errorText")]
        public string ErrorText { get; set; }

    }
}
