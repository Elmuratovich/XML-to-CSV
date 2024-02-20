using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using OfficeOpenXml;
using System.Reflection.PortableExecutable;

namespace XMLtoCSV.Controllers
{
    [ApiController]
    public class FilesController : ControllerBase
    {
        [HttpGet]
        [Route("ReadFile")]
        public async Task<IActionResult> UploadFile()
        {
            string xmlFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\Files");
            string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\Files", "FULL_SNG_KRG_ALL_MN_OPOV_VG.csv");
            int fileCount = 0;
            string[] headers = { "DTREG_GIZ", "TMREG_GIZ", "DTIZM_GIZ", "TMIZM_GIZ", "ID", "KZAV", "NSYST_GIZ",
                              "P0", "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P5_1", "P6_1", "P7_1", "P8",
                              "P9", "P10D", "P10M", "P10G", "P11", "P12", "P13", "P14", "P15", "P16", "P17D",
                              "P17G", "P17M", "P18", "P19", "P20", "P21", "P22", "P23", "P24", "P25D", "P25M",
                              "P25G", "P26D", "P26M", "P26G", "P27", "P28D", "P28M", "P28G", "P29", "P30", "P31",
                              "P32", "P33D", "P33M", "P33G", "P34D", "P34M", "P34G", "P35", "P36", "P37", "P38", "P39",
                              "P40", "P41", "PP0", "PP10", "PP13D", "PP13M", "PP13G", "PP18", "PP21", "PP22", "PP23",
                              "D_CREATE", "LAST_MOD", "NOM_DELT", "TEXT_DELT", "PREQ", "POKR", "KOD_OBJAV", "KOD_PREK", "SIGN", "EfoID", "DT_IZM", "IMG" };
            try
            {


                using (StreamWriter writer = new StreamWriter(csvFilePath))
                {
                    writer.WriteLine(string.Join("|", headers));
                    foreach (string xmlFile in Directory.GetFiles(xmlFolderPath, "*.xml"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(xmlFile);
                        
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        using (StreamReader reader = new StreamReader(xmlFile, Encoding.GetEncoding("iso-8859-5")))
                        {
                            XDocument doc = XDocument.Load(reader);

                            try
                            {
                                var dataElements = doc.Descendants("DOC_MN_OPOV_VG").ToList();
                                if (dataElements.Any())
                                {
                                    for (int j = 0; j < dataElements.Count; j++)
                                    {

                                        string dt_izm = "";
                                        string img_name = "";
                                        string[] row = headers.Select(key =>
                                        {
                                            string value = dataElements[j].Elements().Any(e => e.Name.LocalName == key) ? dataElements[j].Element(key)?.Value ?? "" : "";
                                            return System.Security.SecurityElement.Escape(value);
                                        }).ToArray();

                                        XElement citizenchip = dataElements[j].Elements().FirstOrDefault(t => t.Name.LocalName == "P38");
                                        XElement activ = dataElements[j].Elements().FirstOrDefault(t => t.Name.LocalName == "PP0");

                                        if (citizenchip == null && activ == null)
                                        {
                                            XElement el = dataElements[j].Elements().FirstOrDefault(t => t.Name.LocalName == "PHOTOS");
                                            if (el != null)
                                            {
                                                var images = ConvertImageToBase64(dataElements[j]);
                                                if (images.Count() > 0)
                                                {
                                                    XElement xml_id = dataElements[j].Elements().FirstOrDefault(t => t.Name == "ID");
                                                    foreach (var val in images)
                                                    {
                                                        row[row.Length - 3] = xml_id.Value;
                                                        row[row.Length - 2] = val.DT_IZM;
                                                        row[row.Length - 1] = val.Name;

                                                        writer.WriteLine(string.Join("|", row));
                                                    }
                                                }
                                            }
                                        }
                                        else if (activ == null && citizenchip != null)
                                        {
                                            if (!citizenchip.Value.Equals("КЫРГЫЗСТАН"))
                                            {
                                                XElement el = dataElements[j].Elements().FirstOrDefault(t => t.Name.LocalName == "PHOTOS");
                                                if (el != null)
                                                {
                                                    var images = ConvertImageToBase64(dataElements[j]);
                                                    if (images.Count() > 0)
                                                    {
                                                        XElement xml_id = dataElements[j].Elements().FirstOrDefault(t => t.Name == "ID");
                                                        foreach (var val in images)
                                                        {
                                                            row[row.Length - 3] = xml_id.Value;
                                                            row[row.Length - 2] = val.DT_IZM;
                                                            row[row.Length - 1] = val.Name;

                                                            writer.WriteLine(string.Join("|", row));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("No 'Data' elements found in the XML document.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error row " + ex.Message);
                            }
                        }

                        fileCount += 1;

                        Console.WriteLine($"File {fileName}.xml successfully converted time {DateTime.Now.ToString("dd-MM-yyyy HH:mm")}");
                    }
                }

                Console.WriteLine("CSV file created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return Ok("XML content read and output to console successfully.");
        }


        private static List<ImageItem> ConvertImageToBase64(XElement imageElement)
        {
            List<ImageItem> imageNames = new List<ImageItem>();
            try
            {
                XElement xml_id = imageElement.Elements().FirstOrDefault(t => t.Name == "ID");

                XElement photos = imageElement.Elements().FirstOrDefault(t => t.Name == "PHOTOS");
                if (photos != null)
                {
                    IEnumerable<XElement> photos_items = photos.Elements().Where(t => t.Name == "PHOTOS_ITEM"); // Remove .ToArray()

                    if (photos_items.Count() > 0)
                    {
                        int i = 1;
                        foreach (var photo in photos_items)
                        {
                            XElement img = photo.Elements().FirstOrDefault(t => t.Name == "IMG");
                            XElement dt_izm = photo.Elements().FirstOrDefault(t => t.Name == "DT_IZM");

                            string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\Files\\photos", $"{xml_id.Value}_{i}.jpg");
                            string base64String = img.Value; // Use img[i] instead of img[0]

                            try
                            {
                                // Convert Base64 string to byte array
                                byte[] imageBytes = Convert.FromBase64String(base64String);

                                // Create a MemoryStream from the byte array
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    // Create an Image from the MemoryStream
                                    using (System.Drawing.Image image = System.Drawing.Image.FromStream(ms))
                                    {
                                        // Save the image to a file (optional)
                                        image.Save(csvFilePath, ImageFormat.Jpeg);
                                    }
                                }

                                // Append the image file path and DT_IZM value to the list
                                var item = new ImageItem
                                {
                                    DT_IZM = dt_izm.Value,
                                    Name = $"photos/{xml_id.Value}_{i}.jpg",
                                };

                                imageNames.Add(item);
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions appropriately (e.g., log the error)
                                Console.WriteLine($"Error converting Base64 to image: {xml_id.Value} _ {ex.Message}");
                            }
                            i += 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Image Error " + ex.Message);
            }

            return imageNames;
        }


        public class ImageItem
        {
            public string DT_IZM { get; set; }
            public string Name { get; set; }
        }
    }
}
