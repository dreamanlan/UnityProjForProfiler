input
{
  string("excel", "Scene.xls"){
  	file("xls", "../../Product/Excel");
  };
  string("sheetname", "sceneinfo"){
  	excel_sheets;
  };
  int("skiprows", 3);
  int("skipcols", 2);
  int("fieldrow", 3);
  bool("isGameTable", True);
  string("fields", "");
	string("contains", "");
	string("notcontains1", "");
	string("notcontains2", "");
	feature("source", "excel");
	feature("menu", "8.Excel/find excel");
	feature("description", "just so so");
}
filter
{
  String = gettype("System.String");
	order = row.RowNum;
	if(isGameTable){
		if(isnullorempty(fields)){
			$v1 = [];
			$v2 = findcellindexes(sheet.GetRow(fieldrow), $v1, "*+", sheet.GetRow(0), sheet.GetRow(1));
			$v0 = rowtoline(row, skipcols, $v2);
		}else{
			$v1 = stringsplit(fields,[","]);
			$v2 = findcellindexes(sheet.GetRow(fieldrow), $v1, "*+", sheet.GetRow(0), sheet.GetRow(1));
			$v0 = rowtoline(row, skipcols, $v2);
		};
	}else{
		if(isnullorempty(fields)){
			$v1 = [];
			$v2 = findcellindexes(sheet.GetRow(fieldcol), $v1);
			$v0 = rowtoline(row, skipcols, $v2);
		}else{
			$v1 = stringsplit(fields,[","]);
			$v2 = findcellindexes(sheet.GetRow(fieldcol), $v1);
			$v0 = rowtoline(row, skipcols, $v2);
		};
	};
	if(isGameTable && (getcellstring(row,0)=='*' || getcellstring(row,1)=='+') || !isGameTable){
		if($v0.Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !$v0.Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !$v0.Contains(notcontains2))){
			info = $v0;
		  value = 0;
		  1;
		}else{
		  0;
		};
	}else{
		0;
	};
};