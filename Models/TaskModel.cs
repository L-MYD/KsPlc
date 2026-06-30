using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models
{
    public class TaskModel
    {
        public int id { get; set; }
        public string WCSTaskId { get; set; }      
        public string WMSTaskId { get; set; }     
        public string ThirdPartyTaskId { get; set; }        
        public string TaskType { get; set; }   
        public string ContainerCode { get; set; }        
        public string StartPosition { get; set; }   
        public string AGVCode { get; set; }      
        public string TargetPosition { get; set; }        
        public string TaskStatus { get; set; }  
        public string Progress { get; set; }   
        public string RcsOrTes { get; set; }  
        public string TaskCreaTime { get; set; }  
        public string TaskCompletionTime { get; set; }
        public string Priority { get; set; }
        public string Track { get; set; } 
        public string BackUp { get; set; } 
    }
}