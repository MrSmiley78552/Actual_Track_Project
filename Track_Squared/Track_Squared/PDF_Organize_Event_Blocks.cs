using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Track_Squared
{
    class PDF_Organize_Event_Blocks
    {
        public PDF_Organize_Event_Blocks(string file_path)
        {
            ArrayList event_block_rectangles = new ArrayList();
            PdfReader reader = new PdfReader(file_path);
            float y_coordinate_bottom_of_header_block = new PDF_Organize_Header_Block(file_path).y_coord_bottom_of_header;
            

        }

        private ArrayList locate_event_titles(float llx, float lly, float urx, float ury, int page_num, PdfReader reader)
        {
            ArrayList top_y_coords_for_event_titles = new ArrayList();
            ArrayList event_list = get_event_list();
            int lower_limit_of_current_rect = Convert.ToInt32(ury);
            string event_name = "";
            bool new_event_found = false;

            while(lower_limit_of_current_rect < lly)
            {
                for (int i = lower_limit_of_current_rect; i < lly; i += 5)
                {
                    var test_area_rectangle = new Rectangle(llx, ury - i, urx, ury);
                    foreach (string track_and_field_event in event_list)
                    {
                        if (does_rectangle_contain_specified_text(reader, test_area_rectangle, track_and_field_event, false, page_num) == true)
                        {
                            lower_limit_of_current_rect = i;
                            event_name = track_and_field_event;
                            new_event_found = true;
                            break;
                        }
                    }
                }
                if (new_event_found == true)
                {
                    for (int j = lower_limit_of_current_rect - 25; j > lower_limit_of_current_rect; j++)
                    {
                        var test_area_rectangle = new Rectangle(llx, lower_limit_of_current_rect, urx, j);
                        if (does_rectangle_contain_specified_text(reader, test_area_rectangle, event_name, false, page_num) == false)
                        {
                            top_y_coords_for_event_titles.Add(j - 1);
                            break;
                        }
                    }
                }
            }
            return top_y_coords_for_event_titles;
        }

        /// <summary>
        /// Checks if there are two columns in the PDF results area of the given page.
        /// </summary>
        /// <param name="reader">A PdfReader.</param>
        /// <param name="y_coordinate_bottom_of_header_block">The y-coordinate of the bottom of the header block.</param>
        /// <param name="page_num">The number of the desired page to be checked.</param>
        /// <returns>True if there are two columns.</returns>
        private bool is_two_columns(PdfReader reader, float y_coordinate_bottom_of_header_block, int page_num)
        {
            var center_rectangle = new Rectangle(reader.GetPageSize(1).Width / 2 - 3, 0, reader.GetPageSize(1).Width / 2 + 3, y_coordinate_bottom_of_header_block);

            return does_rectangle_contain_specified_text(reader, center_rectangle, "", true, page_num);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="rect"></param>
        /// <param name="text_to_be_checked"></param>
        /// <param name="equal_text"></param>
        /// <param name="page_num"></param>
        /// <returns></returns>
        private bool does_rectangle_contain_specified_text(PdfReader reader, Rectangle rect, string text_to_be_checked, bool equal_text, int page_num)
        {
            var render_filter = new RenderFilter[1];
            render_filter[0] = new RegionTextRenderFilter(rect);
            var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
            var rect_text = PdfTextExtractor.GetTextFromPage(reader, page_num, text_extraction_strategy);

            if(equal_text == true)
            {
                if (rect_text.Equals(text_to_be_checked))
                    return true;
                else
                    return false;
            }
            else
            {
                if (rect_text.Contains(text_to_be_checked))
                    return true;
                else
                    return false;
            }
        }

        private ArrayList get_event_list()
        {
            ArrayList event_list = new ArrayList();
            SqlDataReader sql_reader = create_sql_query("SELECT Event FROM All_Events");
            if (sql_reader.HasRows)
            {
                while (sql_reader.Read())
                {
                    event_list.Add(sql_reader.GetString(0));
                }
                return event_list;
            }
            else
            {
                // Need an error to be thrown.
            }
        }

        private SqlDataReader create_sql_query(string sql_command)
        {
            SqlConnection sql_connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Track Project\New_Program\Track_Squared\Track_Squared\Track_And_Field_Events.mdf;Integrated Security=True;Connect Timeout=30");
            sql_connection.Open();
            SqlCommand command = new SqlCommand();
            command.CommandText = sql_command;
            command.CommandType = System.Data.CommandType.Text;
            command.Connection = sql_connection;
            return command.ExecuteReader();
        }
    }
}
