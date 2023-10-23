'''import subprocess
meta_data = subprocess.check_output(["netsh", "wlan", "show", "network", "mode=Bssid"])
print(meta_data)'''


import subprocess

output = subprocess.check_output(["netsh", "wlan", "show", "network", "mode=Bssid"]).decode('ascii')
clean_output = output.strip()
new_output = ""

for line in clean_output.split("\n"):
    if "Signal" in line:
        signal_strength = int(line.split(":")[1].strip()[:-1])
        num_stars = int(signal_strength/10)
        new_output += "\t\t Signal\t\t\t\t: " + "■ "*num_stars + "\n"
    else:
        new_output += line + "\n"

print(new_output)







