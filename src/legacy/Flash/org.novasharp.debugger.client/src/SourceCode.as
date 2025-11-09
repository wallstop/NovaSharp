package
{
	public class SourceCode
	{
		private var _Id : int;
		private var _Name : String;
		private var _Lines : Number;
		private var _Text : String;
		private var _CumulativeLength : Vector.<int> = new Vector.<int>();
		
		public var Breakpoints : Vector.<Highlight> = new Vector.<Highlight>();
		
		public function SourceCode(xml: XML)
		{
			m_Id = xml.@id;
			m_Name = xml.@name.toString();
			
			var lines : XMLList = xml.elements();
			
			m_Text = "";
			
			for each (var line : XML in lines)
			{
				m_CumulativeLength.push(_Text.length);
				m_Text += line.toString() + "\n";			
				m_Lines += 1;
			}
		}
		
		public function getId() : int
		{
			return _Id;	
		}
		
		public function getName() : String
		{
			return _Name;	
		}
		
		public function getText() : String
		{
			return _Text;	
		}
		
		public function flattenLocation(line: int, col: int) : int
		{
			return _CumulativeLength[line] + col;
		}
		
		
		public function inflateLocationLine(pos : int) : int 
		{
			for(var line:int = 0; line < _CumulativeLength.length; line++)
			{
				if (pos < _CumulativeLength[line])
					return line - 1;
			}
			
			return _CumulativeLength.length - 1;
		}
		
		public function inflateLocationColumn(pos : int, line : int) : int
		{
			if (line <= 0) return pos;
			return pos - _CumulativeLength[line];
		}
		
	}
}














