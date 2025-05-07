import tkinter as tk
from tkinter import ttk
import subprocess
import threading
import os
import signal
import pyaudio

# ---------- utilidades de áudio ----------
p = pyaudio.PyAudio()

def listar_saidas_audio():
    """Lista somente dispositivos de saída (alto-falantes / fones)."""
    saidas = []
    for i in range(p.get_device_count()):
        info = p.get_device_info_by_index(i)
        if info["maxOutputChannels"] > 0:
            saidas.append(info["name"])
    return saidas

def nome_saida_padrao():
    """Retorna o nome da saída de áudio padrão do Windows."""
    return p.get_default_output_device_info()["name"]

# ---------- callbacks ----------
processo_lokiaudio = None  # será preenchido ao ativar

def ativar():
    global processo_lokiaudio
    s1 = combo1.get()
    s2 = combo2.get()
    # roda em thread separada para não travar a GUI
    def _run():
        global processo_lokiaudio
        processo_lokiaudio = subprocess.Popen(["LokiAudio.exe", s1, s2])
    threading.Thread(target=_run, daemon=True).start()

def desativar():
    if processo_lokiaudio and processo_lokiaudio.poll() is None:
        try:
            os.kill(processo_lokiaudio.pid, signal.SIGTERM)
        except Exception:
            pass   # ignora erros se o processo já saiu
    else:
        print("LokiAudio.exe não está em execução.")

# ---------- interface ----------
root = tk.Tk()
root.title("Selecionador de Saídas de Áudio")
root.geometry("600x400")

label1 = tk.Label(root, text="Saída 1 (padrão):")
label1.pack(pady=(20,0))

lista_saidas = listar_saidas_audio()
combo1 = ttk.Combobox(root, values=lista_saidas, width=60)
combo1.pack()
combo1.set(nome_saida_padrao())        # seleciona a saída padrão

label2 = tk.Label(root, text="Saída 2:")
label2.pack(pady=(20,0))

combo2 = ttk.Combobox(root, values=lista_saidas, width=60)
combo2.pack()

btn_ativar = tk.Button(root, text="Ativar", command=ativar, width=20)
btn_ativar.pack(pady=20)

btn_desativar = tk.Button(root, text="Desativar", command=desativar, width=20)
btn_desativar.pack()

root.mainloop()
