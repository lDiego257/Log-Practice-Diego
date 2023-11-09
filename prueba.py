import re
import json
from datetime import datetime

log_data = []

current_entry = {}
current_error_message = []

with open('pentaho_log.txt', 'r') as log_file:
    for line in log_file:
        line = line.strip()

        if not line:
            continue
        # Extract log level
        match = re.match(r'^(\w+):', line)
        if match:
            log_level = match.group(1)
            current_entry['log_level'] = log_level

        # Append to the log message (error message)
        current_error_message.append(line.strip())

        # Extract log datetime
        datetime_match = re.match(r'(\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}) -', line)
        if datetime_match:
            log_datetime_str = datetime_match.group(1)
            log_datetime = datetime.strptime(log_datetime_str, '%Y/%m/%d %H:%M:%S')
            current_entry['log_datetime'] = log_datetime.strftime('%Y-%m-%d %H:%M:%S')

        # Extract process name
        process_name_match = re.search(r'/job:"(.*?)"', line)
        if process_name_match:
            process_name = process_name_match.group(1)
            current_entry['process_name'] = process_name

 
        # If we find "ERROR:" it indicates the end of the entry
        if "ERROR:" in line:
            log_message = (current_error_message)
            current_entry['log_message'] = log_message
            log_data.append(current_entry)
            current_entry = {}
            current_error_message = []

# Convert the log_data list to a JSON string
log_json = json.dumps(log_data, indent=2)

# Optionally, save the JSON to a file
with open('parsed_pentaho_log.json', 'w') as output_file:
    output_file.write(log_json)

print(log_json)
