using System.Text;

namespace FileTransferConnector
{
    public class FileServiceLocal
    {
        private readonly string filename;
        private readonly string data;
        const string EXPORT_PATH = "C:\\Users\\SANTOSRA1\\Projetos\\";

        public FileServiceLocal(string filename, string data)
        {
            this.filename = filename;
            this.data = data;
        }

        public void SendToFileLocal()
        {
            StringBuilder rootPath = new StringBuilder(EXPORT_PATH);
            string fullPath = $"{EXPORT_PATH}{filename}";
            if (!Directory.Exists(EXPORT_PATH))
                Directory.CreateDirectory(EXPORT_PATH);

            using (StreamWriter writer = new StreamWriter(fullPath, false, Encoding.UTF8))
            {
                writer.Write(data.ToString());
            }

        }

       
    }
}