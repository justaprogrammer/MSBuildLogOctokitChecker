﻿using System;

namespace MSBLOC.Core.Model
{
    public class ProjectPathNotFoundException : ProjectDetailsException
    {
        public ProjectDetails ProjectDetails { get; }
        public string ItemProjectPath { get; }

        public ProjectPathNotFoundException(ProjectDetails projectDetails, string itemProjectPath):
            base($"Project path \"{itemProjectPath}\" is not found")
        {
            ProjectDetails = projectDetails;
            ItemProjectPath = itemProjectPath;
        }
    }
}