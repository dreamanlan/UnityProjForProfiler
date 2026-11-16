input
{
    int("maxSize", 8);
    string("category", "mgroup");
	stringlist("containsany", "");
	stringlist("notcontains", "");
	string("startswith", "");
	string("endswith", "View");
	int("mincount",2);
	float("pathwidth",240){range(20,4096);};
	feature("source", "snapshot");
	feature("menu", "6.Memory/find managed groups");
	feature("description", "just so so");
}
filter
{
    String = gettype("System.String");
	order = group_info.size;
	$v0 = group_info.group;
	$v1 = group_info.count;
	if(group_info.size >= maxSize && string_contains_any($v0,containsany) && string_not_contains($v0,notcontains) && (String.IsNullOrEmpty(startswith) || $v0.StartsWith(startswith)) && (String.IsNullOrEmpty(endswith) || $v0.EndsWith(endswith)) && $v1>=mincount){
		info = format("group:{0} count:{1} size:{2}",
	        group_info.group, group_info.count, group_info.size
	        );
        value = group_info.size;
        set_redirect("FindDsl/memory/find_managed_objects.dsl", "class", group_info.group);
        1;
	}else{
        0;
	};
};