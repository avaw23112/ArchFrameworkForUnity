# UnitFactory ʹ��˵�����ο� ET �� Unit ��ƣ�

- �������� Unit����ǿ�����磩
  ```csharp
  var u = Arch.Net.UnitFactory.CreateUnit(Arch.NamedWorld.DefaultWord,;
                                          });
  ```

- �������� Unit���Զ���ȫ NetworkOwner/NetworkEntityId�������� Unit.UnitId��
  ```csharp
  var uNet = Arch.Net.UnitFactory.CreateNetworkUnit(Arch.NamedWorld.DefaultWord,;
  ```

- ��������ʵ��Ϊ Unit / ���� Unit
  ```csharp
  var e = Arch.NamedWorld.DefaultWord.Create();
  Arch.Net.UnitFactory.EnsureAsUnit(ref e, networked: true);
  ```

- ȫ�� Hook������ͳһ��Ĭ���������¼ͳ�ƻ�󶨱��ֶ���
  ```csharp
  Arch.Net.UnitFactory.GlobalInitHook = ent => {
      // ���磺Ĭ�Ϲ�ĳЩ�������¼������־����� GameObject ����
  };
  ```

