input
{
    string("table", "blocks.txt")
    {
        file("txt");
    };
    string("encoding", "utf-8");
    int("skiprows", 0);
    string("fields", "size_in_bytes,marks_set,objs");
    string("contains", "");
    string("notcontains1", "");
    string("notcontains2", "");
    string("encoding", "utf-8");
    string("style", "grouplist"){
        popup(["itemlist", "grouplist"]);
    };
    float("pathwidth",240){range(20,4096);};
    feature("source", "table");
    feature("menu", "9.Table/group table");
    feature("description", "just so so");
}
filter
{
    order = row.RowIndex;
    if(!is_null_or_empty(fields)){
        $v1 = string_split(fields,[","]);
        $v2 = find_cell_indexes(sheet.GetRow(0), $v1);
        $v0 = row_to_line(row, 0, $v2);
        if(list_size($v2)>=3 && $v0.Contains(contains) && (is_null_or_empty(notcontains1) || !$v0.Contains(notcontains1)) && (is_null_or_empty(notcontains2) || !$v0.Contains(notcontains2))){
            asset_path = get_cell_string(row, $v2[0]);
            scene_path = "";
            info = $v0;
            value = get_cell_numeric(row, $v2[2]);
            group = asset_path;
            1;
        }else{
            0;
        };
    }else{
        0;
    };
}
group
{
    order = sum;
    value = sum;
    info = format("{0}=>{1}", group, count);
    1;
};