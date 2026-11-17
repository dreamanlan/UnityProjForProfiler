input
{
    string("table", ""){
        file("csv");
    };
    table("table2", ""){
        encoding("utf-8");
        file("csv");
    };
    string("encoding", "utf-8");
    int("skiprows", 0);
    stringlist("contains", "");
    stringlist("notcontains", "");
    int("mindiff", 0);
    float("minmultiple", 0);
    int("minsizediff", 0);
    int("resultopt",1){
        toggle(["all","onlyalone","onlynotalone"],[1,2,3]);
    };
    float("pathwidth",240){range(20,4096);};
    feature("source", "table");
    feature("menu", "9.Table/compare memory group");
    feature("description", "just so so");
}
filter
{
    String = get_type("System.String");
    $header = sheet.GetRow(0);
    $ix = find_cell_index($header, "type");
    $ix2 = find_cell_index($header, "count");
    $ix3 = find_cell_index($header, "size");

    $type = get_cell_string(row, $ix);
    $count = get_cell_numeric(row, $ix2);
    $size = get_cell_numeric(row, $ix3);

    $v99 = 0;
    if(string_contains($type, contains) && string_not_contains($type, notcontains)){
        $rowIndex = find_row_index(table2, $ix, $type);
        if($rowIndex>0 && resultopt!=2){
            $row = table2.GetRow($rowIndex);
            $count2 = get_cell_numeric($row, $ix2);
            $size2 = get_cell_numeric($row, $ix3);
            if(abs($count-$count2)>=mindiff && ($count*minmultiple<=$count2 || $count2*minmultiple<=$count) && abs($size-$size2)>=minsizediff){
                info=format("{0}, count {1}=>{2}, size {3}=>{4}",$type,$count,$count2,$size,$size2);
                order = $size;
                value = $size-$size2;
                $v99=1;
            };
        };
        if($rowIndex<=0 && resultopt!=3){
            if($count>=mindiff && $size>=minsizediff){
                info=format("{0}, count {1}=>{2}, size {3}=>{4}",$type,$count,0,$size,0);
                order = $size;
                value = $size;
                $v99=1;
            };
        };
    };
    $v99;
};
