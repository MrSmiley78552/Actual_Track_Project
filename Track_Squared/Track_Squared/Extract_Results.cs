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
    class Extract_Results
    {
        public Extract_Results(string file_path)
        {
            PDF_Organize_Body_Block body_block = new PDF_Organize_Body_Block(file_path);
            int number_of_columns = body_block.number_of_columns;
            float lower_y_coordinate_of_header_block = body_block.lower_y_coordinate_of_header_block;
            string[] body_block_lines = body_block.body_block_lines;
            string[] current_column_keywords = new string[] { "new" };
            string current_event_title = "new";
            Boolean is_finals = true;
            Boolean is_new_column_keywords = true;

            for(int i = 0; i < body_block_lines.Length; i++)
            {
                analyze_line(body_block_lines[i], current_column_keywords, current_event_title, is_finals, is_new_column_keywords);
            }

        }

        private void analyze_line(string line, string[] current_column_keywords, string current_event_title, Boolean is_finals, Boolean is_new_column_keywords)
        {
            if(does_line_contain_event_title(line) == true)
            {
                current_event_title = get_event_title_from_line(line);
                is_new_column_keywords = false;
            }
            else if(does_line_contain_column_keywords(line) == true)
            {
                if (get_column_keywords(line).Length >= 2)
                {
                    // it's a full row of keywords
                    current_column_keywords = get_column_keywords(line);
                    is_new_column_keywords = true;
                    // Probably where we want to find the rectangle sizes
                }
                else
                {
                    // it's a row specifying prelims or finals
                    if (get_column_keywords(line)[0].Equals("Finals"))
                        is_finals = true;
                    else
                        is_finals = false;
                }
            }
            else if(is_new_column_keywords == true)
            {
                // it's a row of results
                if(current_event_title.Contains("Relay"))
                {
                    if (is_column_rectangles() == false)
                        set_column_rectangles();
                    // handle the relay

                }
                else
                {
                    // handle the singles event
                    handle_singles_event(line);
                }
            }
        }

        private void form_rectangles(int page_number, int number_of_columns, PdfReader reader, float lower_y_coordinate_of_header_block, string corresponding_line, int y_down_iterator, string[] current_column_keywords)
        {
            ArrayList column_coordinates = new ArrayList();

            if (number_of_columns == 2)
            {
                Boolean do_lines_match = false;
                Boolean looking_at_left_column = true;
                float middle_coordinate = reader.GetPageSize(page_number).Width / 2;
                float ten_up_from_bottom_of_page = reader.GetPageSize(page_number).Height - 10;

                // Start with the left side of the page
                float left_side_of_rectangle = 0;
                float right_side_of_rectangle = middle_coordinate;
                float top_of_rectangle = lower_y_coordinate_of_header_block;
                float bottom_of_rectangle = lower_y_coordinate_of_header_block - 14;

                // Finds the rectangle that contains all of the text from the corresponding line in the arrary.
                // Will want to return the y_down_iterator, so it starts correctly if the second column is in use.
                while (do_lines_match == false)
                {
                    left_side_of_rectangle = 0;
                    right_side_of_rectangle = middle_coordinate;
                    top_of_rectangle = top_of_rectangle - y_down_iterator;
                    bottom_of_rectangle = bottom_of_rectangle - y_down_iterator;

                    Rectangle single_row_rectangle = new Rectangle(left_side_of_rectangle, bottom_of_rectangle, right_side_of_rectangle, top_of_rectangle);
                    var render_filter = new RenderFilter[1];
                    render_filter[0] = new RegionTextRenderFilter(single_row_rectangle);
                    var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
                    var single_row_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, page_number, text_extraction_strategy);

                    if (corresponding_line.Equals(single_row_rectangle_text))
                        do_lines_match = true;
                    else if(bottom_of_rectangle >= ten_up_from_bottom_of_page && looking_at_left_column == true)
                    {
                        left_side_of_rectangle = middle_coordinate;
                        right_side_of_rectangle = reader.GetPageSize(page_number).Width;
                        y_down_iterator = 0;
                        looking_at_left_column = false;
                    }
                    else if(bottom_of_rectangle >= ten_up_from_bottom_of_page && looking_at_left_column == false)
                    {
                        left_side_of_rectangle = 0;
                        right_side_of_rectangle = middle_coordinate;
                        y_down_iterator = 0;
                        looking_at_left_column = true;
                        page_number += 1;
                        if(page_number > reader.NumberOfPages)
                        {
                            // Throw an error
                        }
                    }
                }

                // Found a match between lines
                // Gets the coordinates of the left start of each keyword.
                for(int i = 0; i < current_column_keywords.Length; i++)
                {
                    for(int j = 0; j < right_side_of_rectangle; j++)
                    {
                        Rectangle column_finder = new Rectangle(left_side_of_rectangle, bottom_of_rectangle, left_side_of_rectangle + j, top_of_rectangle);
                        var render_filter = new RenderFilter[1];
                        render_filter[0] = new RegionTextRenderFilter(column_finder);
                        var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
                        var column_finder_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, page_number, text_extraction_strategy);

                        char leading_character_of_keyword = current_column_keywords[i].ToCharArray()[0];
                        if(column_finder_rectangle_text.Contains(leading_character_of_keyword))
                        {
                            left_side_of_rectangle = left_side_of_rectangle + j - 10;
                            column_coordinates.Add(left_side_of_rectangle);
                            break;
                        }
                    }
                }

            }
            else
            {

            }
        }

        private Rectangle set_column_rectangles(string file_path, int number_of_columns, string[] column_keywords)
        {
            PdfReader reader = new PdfReader(file_path);
            float page_height = reader.GetPageSize(1).Height;
            float page_width = reader.GetPageSize(1).Width;
            var column_headers_rectangle = new Rectangle(0, 0, 0, 0);
            Boolean rectangle_found = false;

            while(rectangle_found == false)
            {
                for(int i = 0; i < page_height; i++)
                {
                    column_headers_rectangle = new Rectangle(0, page_height - (i + 1) * 10, reader.GetPageSize(1).Width / number_of_columns, page_height - i*10);
                    var render_filter = new RenderFilter[1];
                    render_filter[0] = new RegionTextRenderFilter(column_headers_rectangle);
                    var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
                    var center_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, 1, text_extraction_strategy);

                    int correct_columns_found = 0;
                    for(int j = 0; j < column_keywords.Length; j++)
                    {
                        if (!center_rectangle_text.Contains(column_keywords[j]))
                            break;
                        correct_columns_found++;
                    }
                    if (correct_columns_found == column_keywords.Length)
                    {
                        rectangle_found = true;
                    }
                }
            }
            return column_headers_rectangle;
        }

        private void handle_singles_event(string line)
        {
            string[] line_split = line.Split(' ');
            string place = line_split[0];
            // get name
            // get year (if it's there)
            // get school
        }

        private Boolean does_line_contain_column_keywords(string line)
        {
            Boolean status = false;
            SqlDataReader sql_reader = create_sql_query("SELECT Keyword FROM Column_Keywords");
            if(sql_reader.HasRows)
            {
                while(sql_reader.Read())
                {
                    if (line.Contains(sql_reader.GetString(0)))
                        status = true;
                }
            }
            else
            {
                // Need an error to be thrown.
            }
            sql_reader.Close();
            return status;
        }

        private string[] get_column_keywords(string line)
        {
            List<String> column_keywords = new List<String>();
            SqlDataReader sql_reader = create_sql_query("SELECT Keyword FROM Column_Keywords");
            if (sql_reader.HasRows)
            {
                while (sql_reader.Read())
                {
                    if (line.Contains(sql_reader.GetString(0)))
                        column_keywords.Add(sql_reader.GetString(0));
                }
            }
            else
            {
                // Need an error to be thrown.
            }
            sql_reader.Close();
            return column_keywords.ToArray();
        }

        private Boolean does_line_contain_event_title(string line)
        {
            Boolean status = false;
            SqlDataReader sql_reader = create_sql_query("SELECT Event FROM All_Events");
            if(sql_reader.HasRows)
            {
                while(sql_reader.Read())
                {
                    if (line.Contains(sql_reader.GetString(0)))
                        status = true;
                }
            }
            else
            {
                // Need an error to be thrown.
            }
            sql_reader.Close();
            return status;
        }

        private string get_event_title_from_line(string line)
        {
            string event_title = "";
            SqlDataReader sql_reader = create_sql_query("SELECT Event FROM All_Events");
            if(sql_reader.HasRows)
            {
                while(sql_reader.Read())
                {
                    if(line.Contains(sql_reader.GetString(0)))
                        event_title = sql_reader.GetString(0);
                }
            }
            else
            {
                // Need an error to be thrown.
            }
            sql_reader.Close();
            return event_title;
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
