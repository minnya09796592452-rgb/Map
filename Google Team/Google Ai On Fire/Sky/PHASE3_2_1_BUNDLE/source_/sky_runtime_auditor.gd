@tool
extends Node3D

func _ready() -> void:
	var log_lines: PackedStringArray = []
	log_lines.append('=== RUNTIME RECURSIVE MESH & ERROR AUDIT (GODOT 4.7.2) ===')
	_scan_recursive(self, log_lines)
	var f := FileAccess.open('res://error_logs/RUNTIME_ERROR_VERIFICATION.txt', FileAccess.WRITE)
	if f:
		f.store_string('\n'.join(log_lines) + '\n')
		f.close()
	print('ERROR_LOGS_CAPTURED_OK | LINES=', log_lines.size())

func _scan_recursive(node: Node, out: PackedStringArray) -> void:
	if node is MeshInstance3D:
		var mi := node as MeshInstance3D
		var m: Mesh = mi.mesh
		if m == null:
			out.append('NULL_MESH: ' + str(mi.get_path()))
		else:
			var sc: int = m.get_surface_count()
			out.append('MESH: ' + str(mi.get_path()) + ' | res=' + m.resource_path + ' | surfaces=' + str(sc))
			if sc <= 0:
				out.append('EMPTY_MESH_FOUND: ' + str(mi.get_path()) + ' -> ' + m.resource_path)
	for child in node.get_children():
		_scan_recursive(child, out)
