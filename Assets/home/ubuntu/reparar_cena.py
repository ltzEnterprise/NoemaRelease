import sys

def reparar_cena(caminho_arquivo):
    try:
        with open(caminho_arquivo, 'r', encoding='utf-8') as f:
            linhas = f.readlines()
        
        abertas = 0
        fechadas = 0
        
        for linha in linhas:
            abertas += linha.count('{')
            fechadas += linha.count('}')
        
        faltam = abertas - fechadas
        
        if faltam > 0:
            print(f"Detectado desbalanceamento: {abertas} abertas, {fechadas} fechadas. Adicionando {faltam} chaves ao final.")
            with open(caminho_arquivo, 'a', encoding='utf-8') as f:
                f.write('\n' + ('}' * faltam) + '\n')
            return True
        else:
            print("Nenhum desbalanceamento de chaves detectado no final do arquivo.")
            return False
            
    except Exception as e:
        print(f"Erro ao processar arquivo: {e}")
        return False

if __name__ == "__main__":
    print("Este script tenta fechar chaves YAML pendentes em arquivos .unity corrompidos.")
    print("AVISO: Faça um backup da sua cena antes de usar isso no seu PC!")
