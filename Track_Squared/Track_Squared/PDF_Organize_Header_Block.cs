using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text.RegularExpressions;
using iTextSharp.text;

namespace Track_Squared
{
    class PDF_Organize_Header_Block
    {
        public PDF_Organize_Header_Block(string file_path)
        {
            PdfReader reader = new PdfReader(file_path);
            string[] header_block_lines = get_header_block(reader);
            string meet_date = get_meet_date_from_header_block(header_block_lines);
            string meet_name = get_meet_name_from_header_block(header_block_lines, meet_date);
            float y_coord_bottom_of_header = get_bottom_of_rectangle_around_header_block(reader, header_block_lines);
        }

        public string[] header_block_lines { get; }
        public string meet_date { get; }
        public string meet_name { get; }
        public float y_coord_bottom_of_header { get; }

        /// <summary>
        /// Gets the header block from the first page of the PDF file.
        /// </summary>
        /// <returns>A string array containing all the lines of text in the header block.</returns>
        private string[] get_header_block(PdfReader reader)
        {
            string[] lines_on_page_one = get_lines_on_page(reader, 1);
            int top_of_header_line_num = get_top_of_header_block(lines_on_page_one);
            int bottom_of_header_line_num = get_bottom_of_header_block(lines_on_page_one, top_of_header_line_num);

            return lines_on_page_one.Skip(top_of_header_line_num).Take(bottom_of_header_line_num - top_of_header_line_num + 1).ToArray();
        }

        /// <summary>
        /// Takes the PDF file and gets all the lines on the first page.
        /// </summary>
        /// <returns>A string array containing all the lines of text on the first page.</returns>
        private string[] get_lines_on_page(PdfReader reader, int page_num)
        {
            return PdfTextExtractor.GetTextFromPage(reader, page_num, new LocationTextExtractionStrategy()).Split('\n');
        }

        /// <summary>
        /// Searches the top 10 lines for a date.
        /// If a date is found, that line is used as the top of the header block.
        /// </summary>
        /// <param name="lines_on_page">A string array containing all the lines of text on the page.</param>
        /// <returns>Line number where the date occured.</returns>
        private int get_top_of_header_block(string[] lines_on_page)
        {
            int line_num_output = -1;

            // Regex for: 00/00/0000
            Regex rgx = new Regex(@"\d{1|2}/\d{1|2}/\d{4}");

            for(int line_num = 0; line_num < 10; line_num++)
            {
                if (!rgx.Match(lines_on_page[line_num]).ToString().Equals(""))
                {
                    rgx.m
                    line_num_output = line_num;
                    break;
                }
            }
            return line_num_output;
        }

        /// <summary>
        /// Searches the 10 lines following the top of the header for "Results".
        /// If that is found, then that line is used as the bottom of the header block.
        /// </summary>
        /// <param name="lines_on_page">A string array containing all the lines of text on the page.</param>
        /// <param name="top_of_header_block">The line number for the top of the header block.</param>
        /// <returns>Line number where "Results" occured.</returns>
        private int get_bottom_of_header_block(string[] lines_on_page, int top_of_header_block)
        {
            int line_num_output = -1;
            for(int line_num = top_of_header_block; line_num < top_of_header_block + 10; line_num++)
            {
                if(lines_on_page[line_num].Equals("Results"))
                {
                    line_num_output = line_num;
                    break;
                }
            }
            return line_num_output;
        }

        /// <summary>
        /// Searches the header block for the meet date.
        /// </summary>
        /// <param name="lines_in_header_block">A string array containing all the lines of text in the header block.</param>
        /// <returns>The meet date.</returns>
        private string get_meet_date_from_header_block(string[] lines_in_header_block)
        {
            string meet_date = "";

            // Regex for: 00/00/0000
            Regex rgx = new Regex(@"\d{1|2}/\d{1|2}/\d{4}");

            for(int line_num = 0; line_num < lines_in_header_block.Length; line_num++)
            {
                if (!rgx.Match(lines_in_header_block[line_num]).ToString().Equals(""))
                {
                    meet_date = rgx.Match(lines_in_header_block[line_num]).ToString();
                    break;
                }
            }
            return meet_date;
        }

        /// <summary>
        /// Searches the header block for the meet name.
        /// </summary>
        /// <param name="lines_in_header_block">A string array containing all the lines of text in the header block.</param>
        /// <param name="meet_date">The meet date.</param>
        /// <returns>The meet name.</returns>
        private string get_meet_name_from_header_block(string[] lines_in_header_block, string meet_date)
        {
            string meet_name = "";

            for(int line_num = 0; line_num < lines_in_header_block.Length; line_num++)
            {
                int index_of_hyphen = lines_in_header_block[line_num].IndexOf('-');
                if(index_of_hyphen != -1)
                {
                    meet_name = lines_in_header_block[line_num].Substring(0, index_of_hyphen);
                    break;
                }
            }
            if (!meet_name.Equals(""))
                meet_name = lines_in_header_block[1];
            return meet_name;
        }

        /// <summary>
        /// It searches for the y-coordinate of the bottom of the header block.
        /// Starts at the top of the page and scans down until there is a rectangle from
        /// the top of the pdf down, containing all text in the header block.
        /// </summary>
        /// <param name="reader">A PdfReader for the PDF.</param>
        /// <param name="header_block_lines">The lines of text in the header block.</param>
        /// <returns>The y-coordinate of the bottom of the header block.</returns>
        private float get_bottom_of_rectangle_around_header_block(PdfReader reader, string[] header_block_lines)
        {
            float pdf_page_height = reader.GetPageSize(1).Height;
            float pdf_page_width = reader.GetPageSize(1).Width;
            float bottom_of_header_height = pdf_page_height - 10;

            Boolean does_rectangle_contain_header_block = false;
            var render_filter = new RenderFilter[1];
            var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);

            while (does_rectangle_contain_header_block == false)
            {
                var header_rectangle = new Rectangle(0, bottom_of_header_height, pdf_page_width, pdf_page_height);
                render_filter[0] = new RegionTextRenderFilter(header_rectangle);
                var header_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, 1, text_extraction_strategy);
                string[] header_rectangle_text_array = header_rectangle_text.Split('\n');
                for (int line_num = 0; line_num < header_block_lines.Length; line_num++)
                {
                    if (!header_block_lines[line_num].Equals(header_rectangle_text_array[line_num]))
                    {
                        bottom_of_header_height -= 10;
                        break;
                    }
                    else if (line_num == header_block_lines.Length - 1 && header_block_lines[line_num].Equals(header_rectangle_text_array[line_num]))
                    {
                        does_rectangle_contain_header_block = true;
                    }
                }
            }
            return bottom_of_header_height;
        }
    }
}
