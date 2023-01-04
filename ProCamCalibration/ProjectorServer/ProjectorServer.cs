using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.Windows.Forms;
using System.Collections.Generic;

/*
Generate a client with 
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\x64\SvcUtil.exe" /noConfig /out:ProjectorClient.cs http://localhost:8733/Design_Time_Addresses/ProjectorServer/Service1 /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Drawing.dll"
*/


namespace RoomAliveToolkit
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)] // TODO: revisit mode
    [ServiceContract]
    public class ProjectorServer
    {
        Dictionary<int, ProjectorForm> projectorForms = new Dictionary<int, ProjectorForm>();
        ProjectorServerForm projectorServerForm;

        public ProjectorServer(ProjectorServerForm form)
        {
            this.projectorServerForm = form;
        }


        [OperationContract]
        public void OpenDisplay(int screenIndex)
        {
            if (!projectorForms.ContainsKey(screenIndex))
            {
                var projectorForm = new ProjectorForm(screenIndex);
                projectorForm.Show();
                projectorForm.BringToFront();
                projectorForms[screenIndex] = projectorForm;
            }
        }

        [OperationContract]
        public System.Drawing.Size Size(int screenIndex)
        {
            return Screen.AllScreens[screenIndex].Bounds.Size;
        }

        [OperationContract]
        public int ScreenCount()
        {
            return Screen.AllScreens.Length;
        }

        [OperationContract]
        public void SetColor(int screenIndex, float r, float g, float b)
        {
            var projectorForm = projectorForms[screenIndex];
            projectorForm.SetColor(r, g, b);
        }

        [OperationContract]
        public void DisplayName(int screenIndex, string name)
        {
            var projectorForm = projectorForms[screenIndex];
            projectorForm.DisplayName(name);
        }


        [OperationContract]
        public int NumberOfGrayCodeImages(int screenIndex)
        {
            var projectorForm = projectorForms[screenIndex];
            return projectorForm.NumberOfGrayCodeImages;
        }

        [OperationContract]
        public void DisplayGrayCode(int screenIndex, int i)
        {
            var projectorForm = projectorForms[screenIndex];
            projectorForm.BringToFront();
            projectorForm.DisplayGrayCode(i);
        }

        [OperationContract]
        public void CloseDisplay(int screenIndex)
        {
            if (projectorForms.ContainsKey(screenIndex))
            {
                var projectorForm = projectorForms[screenIndex];
                projectorServerForm.Invoke(new Action(() => projectorForm.Close()));
                projectorForms.Remove(screenIndex);
            }
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var projectorServerForm = new ProjectorServerForm();
            var projectorServer = new ProjectorServer(projectorServerForm);
            var serviceHost = new ServiceHost(projectorServer);

            // discovery
            serviceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            serviceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());

            serviceHost.Open();
            Application.Run(projectorServerForm);
        }
    }
}


