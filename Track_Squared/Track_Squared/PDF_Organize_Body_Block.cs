using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Track_Squared
{
    class PDF_Organize_Body_Block
    {
        public PDF_Organize_Body_Block(string file_path)
        {
            PdfReader reader = new PdfReader(file_path);
            PDF_Organize_Header_Block pdf_organize_header_block = new PDF_Organize_Header_Block(file_path);
            string[] header_block_lines = pdf_organize_header_block.header_block_lines;

            string[] body_block_lines = get_column_lines(reader, pdf_organize_header_block.y_coord_bottom_of_header);
            int number_of_columns = get_number_of_columns(reader, pdf_organize_header_block.y_coord_bottom_of_header);
        }

        public string[] body_block_lines { get; }
        public int number_of_columns { get; }

        /// <summary>
        /// Goes through each page and orders the results area from each page.
        /// </summary>
        /// <param name="reader">A PdfReader.</param>
        /// <param name="header_block_lines">The lines of text in the header block.</param>
        /// <returns>An array of all the results in order.</returns>
        private string[] get_column_lines(PdfReader reader, float y_coordinate_bottom_of_header_block)
        {
            List<String> body_block_text_list = new List<String>();
            if (get_number_of_columns(reader, y_coordinate_bottom_of_header_block) == 2)
            {
                for(int page_number = 1; page_number < reader.NumberOfPages; page_number++)
                {
                    var left_rectangle = new Rectangle(0, 0, reader.GetPageSize(page_number).Width / 2, y_coordinate_bottom_of_header_block);
                    var render_filter = new RenderFilter[1];
                    render_filter[0] = new RegionTextRenderFilter(left_rectangle);
                    var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
                    var left_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, page_number, text_extraction_strategy);

                    var right_rectangle = new Rectangle(reader.GetPageSize(page_number).Width / 2, 0, reader.GetPageSize(page_number).Width, y_coordinate_bottom_of_header_block);
                    render_filter[0] = new RegionTextRenderFilter(right_rectangle);
                    text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
                    var right_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, page_number, text_extraction_strategy);

                    body_block_text_list.AddRange(left_rectangle_text.Split('\n').ToList());
                    body_block_text_list.AddRange(right_rectangle_text.Split('\n').ToList());
                }
            }
            else
            {
                for(int page_number = 1; page_number < reader.NumberOfPages; page_number++)
                {
                    var single_rectangle = new Rectangle(0, 0, reader.GetPageSize(page_number).Width, y_coordinate_bottom_of_header_block);
                    var render_filter = new RenderFilter[1];
                    render_filter[0] = new RegionTextRenderFilter(single_rectangle);
                    var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
                    var single_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, page_number, text_extraction_strategy);
                    body_block_text_list.AddRange(single_rectangle_text.Split('\n').ToList());
                }
            }
            return body_block_text_list.ToArray();
        }

        /// <summary>
        /// Gets the number of columns in the PDF results area.
        /// </summary>
        /// <param name="reader">A PdfReader.</param>
        /// <returns>The number of columns in the PDF results area.</returns>
        private int get_number_of_columns(PdfReader reader, float y_coordinate_bottom_of_header_block)
        {
            if (is_two_columns(reader, y_coordinate_bottom_of_header_block) == true)
                return 2;
            else
                return 1;
        }

        /// <summary>
        /// Checks if there are two columns in the PDF results area.
        /// </summary>
        /// <param name="reader">A PdfReader.</param>
        /// <param name="header_block_lines">The lines of text in the header block.</param>
        /// <returns>True if there are two columns.</returns>
        private bool is_two_columns(PdfReader reader, float y_coordinate_bottom_of_header_block)
        {
            var center_rectangle = new Rectangle(reader.GetPageSize(1).Width / 2 - 3, 0, reader.GetPageSize(1).Width / 2 + 3, y_coordinate_bottom_of_header_block);
            var render_filter = new RenderFilter[1];
            render_filter[0] = new RegionTextRenderFilter(center_rectangle);
            var text_extraction_strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), render_filter);
            var center_rectangle_text = PdfTextExtractor.GetTextFromPage(reader, 1, text_extraction_strategy);

            if (center_rectangle_text.Equals(""))
                return true;
            else
                return false;
        }

        
    }
}
