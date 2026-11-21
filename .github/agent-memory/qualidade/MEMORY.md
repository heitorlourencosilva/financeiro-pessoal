# Revisões de qualidade

- Em 2026-11-21: revisão confirmou build verde, 192 testes verdes e Docker 29.9.2 disponível.
- Warning pendente: NU1902 sobre `AngleSharp 1.2.0` (dependência transitiva dos testes, via bunit/AngleSharp.Diffing).
- Verificar sempre diffs locais antes de atribuir mudanças à validação; neste estado havia muitos arquivos modificados/adicionados não relacionados à execução dos testes.
