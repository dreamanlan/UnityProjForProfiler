input("MapSymbols")
{
    string("txtfile", ""){
        file("txt");
    };
    string("pattern1", "");
    string("subst1", "");
    string("pattern2", "");
    string("subst2", "");
    string("pattern3", "");
    string("subst3", "");
    string("pattern4", "");
    string("subst4", "");
    string("pattern5", "");
    string("subst5", "");
    string("pattern6", "");
    string("subst6", "");
    string("pattern7", "");
    string("subst7", "");
    string("pattern8", "");
    string("subst8", "");
	feature("source", "list");
	feature("menu", "1.Tools/Subst");
	feature("description", "just so so");
}
process
{
    $lines=read_all_lines(txtfile);
    if(!is_null_or_empty(pattern1)){
        $lines = subst($lines, pattern1, subst1);
    };
    if(!is_null_or_empty(pattern2)){
        $lines = subst($lines, pattern2, subst2);
    };
    if(!is_null_or_empty(pattern3)){
        $lines = subst($lines, pattern3, subst3);
    };
    if(!is_null_or_empty(pattern4)){
        $lines = subst($lines, pattern4, subst4);
    };
    if(!is_null_or_empty(pattern5)){
        $lines = subst($lines, pattern5, subst5);
    };
    if(!is_null_or_empty(pattern6)){
        $lines = subst($lines, pattern6, subst6);
    };
    if(!is_null_or_empty(pattern7)){
        $lines = subst($lines, pattern7, subst7);
    };
    if(!is_null_or_empty(pattern8)){
        $lines = subst($lines, pattern8, subst8);
    };
    $dir = get_directory_name(txtfile);
    $filename = get_file_name_without_extension(txtfile);
    $file = combine_path($dir,$filename+"_subst.txt");
    write_all_lines($file,$lines);
};