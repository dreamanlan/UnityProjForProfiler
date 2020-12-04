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
			var(1) = [];
			var(2) = findcellindexes(sheet.GetRow(fieldrow), var(1), "*+", sheet.GetRow(0), sheet.GetRow(1));
			var(0) = rowtoline(row, skipcols, var(2));
		}else{
			var(1) = stringsplit(fields,[","]);
			var(2) = findcellindexes(sheet.GetRow(fieldrow), var(1), "*+", sheet.GetRow(0), sheet.GetRow(1));
			var(0) = rowtoline(row, skipcols, var(2));
		};
	}else{
		if(isnullorempty(fields)){
			var(1) = [];
			var(2) = findcellindexes(sheet.GetRow(fieldcol), var(1));
			var(0) = rowtoline(row, skipcols, var(2));
		}else{
			var(1) = stringsplit(fields,[","]);
			var(2) = findcellindexes(sheet.GetRow(fieldcol), var(1));
			var(0) = rowtoline(row, skipcols, var(2));
		};
	};
	if(isGameTable && (getcellstring(row,0)=='*' || getcellstring(row,1)=='+') || !isGameTable){
		if(var(0).Contains(contains) && (String.IsNullOrEmpty(notcontains1) || !var(0).Contains(notcontains1)) && (String.IsNullOrEmpty(notcontains2) || !var(0).Contains(notcontains2))){
			info = var(0);
		  value = 0;
		  1;
		}else{
		  0;
		};
	}else{
		0;
	};
};