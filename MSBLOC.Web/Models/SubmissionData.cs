﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MSBLOC.Web.Models
{
    public class SubmissionData
    {
        public string ApplicationOwner { get; set; }
        public string ApplicationName { get; set; }
        public string CommitSha { get; set; }
        public string CloneRoot { get; set; }
        public string BinaryLogFile { get; set; }
    }
}
