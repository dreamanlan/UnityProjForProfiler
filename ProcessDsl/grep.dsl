input("MapSymbols")
{
    string("txtfile", ""){
        file("txt");
    };
    string("filter1", "");
    string("filter2", "");
    string("filter3", "");
    string("filter4", "");
    string("filter5", "");
    string("filter6", "");
    string("filter7", "");
    string("filter8", "");
	feature("source", "list");
	feature("menu", "1.Tools/Grep");
	feature("description", "just so so");
}
process
{
    $lines=read_all_lines(txtfile);
    if(!is_null_or_empty(filter1)){
        $lines = grep($lines, filter1);
    };
    if(!is_null_or_empty(filter2)){
        $lines = grep($lines, filter2);
    };
    if(!is_null_or_empty(filter3)){
        $lines = grep($lines, filter3);
    };
    if(!is_null_or_empty(filter4)){
        $lines = grep($lines, filter4);
    };
    if(!is_null_or_empty(filter5)){
        $lines = grep($lines, filter5);
    };
    if(!is_null_or_empty(filter6)){
        $lines = grep($lines, filter6);
    };
    if(!is_null_or_empty(filter7)){
        $lines = grep($lines, filter7);
    };
    if(!is_null_or_empty(filter8)){
        $lines = grep($lines, filter8);
    };
    $dir = get_directory_name(txtfile);
    $filename = get_file_name_without_extension(txtfile);
    $file = combine_path($dir,$filename+"_grep.txt");
    write_all_lines($file,$lines);
};