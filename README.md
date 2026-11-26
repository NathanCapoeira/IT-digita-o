# IT Digitação 3.0 — reconstrução WPF

Aplicativo educacional de treino de digitação do Instituto de Transformação. A reconstrução em andamento usa WPF, conteúdo JSON e recursos gráficos locais, sem login, telemetria, publicidade ou conexão necessária durante o uso.

## Estado da reconstrução

O trabalho da versão WPF está na branch `rebuild/it-digitacao`; a branch `main` continua preservando a versão anterior. Esta branch contém as telas e o fluxo de compilação WPF, mas a compilação Windows e a validação visual ainda precisam ser confirmadas antes de distribuir o aplicativo.

## Requisitos

- Windows e .NET Framework 4.8 para executar.
- Visual Studio ou Build Tools com MSBuild e o targeting pack do .NET Framework 4.8 para compilar.
- Para Windows 7 SP1, instale previamente todos os requisitos e atualizações exigidos pelo instalador do .NET Framework 4.8. A compatibilidade com Windows 7 ainda não foi testada nesta reconstrução.

## Compilar no Windows

Na raiz do repositório, abra o Developer PowerShell do Visual Studio e execute:

```powershell
.\scripts\build-windows.cmd
```

O script restaura e compila o projeto WPF em Release x86 e abre o executável gerado em:

```text
src\ITDigitacao.Wpf\bin\x86\Release\net48\IT-Digitacao-3.0.exe
```

O workflow `.github/workflows/build-windows.yml` compila o WPF, executa os testes do Core, verifica os recursos e prepara o pacote `IT-Digitacao-3.0-Rebuild-Release.zip`. Uma configuração de workflow não comprova que a execução terminou com sucesso; consulte a aba Actions para o resultado real.

## Conteúdo e funcionamento offline

As 30 atividades ficam em `content/activities.json`, em UTF-8, organizadas em dez categorias e três níveis. Imagens, ícones, logo e mascotes ficam em `src/ITDigitacao.Wpf/Assets` e são incorporados pelo projeto WPF.

## Limitações conhecidas

- Build Release WPF em Windows ainda pendente de confirmação.
- Testes automatizados foram acrescentados, mas ainda precisam de execução confirmada.
- Validação visual e funcional nas resoluções e níveis de DPI previstos ainda pendente.
- Compatibilidade com Windows 7 ainda não comprovada em equipamento real.
- Os textos são autorais e precisam de revisão factual/pedagógica antes do uso institucional.
- Confirme a autorização de uso do logotipo oficial antes da distribuição.
